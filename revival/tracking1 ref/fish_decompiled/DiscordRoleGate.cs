using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class DiscordRoleGate
{
	private const string DiscordClientId = "1514731651823964270";
	private const string RequiredGuildId = "1507846607993831655";
	private const string RequiredRoleId = "1508541637897224402";
	private const string RedirectUri = "http://localhost:9999/callback";
	private const string AuthUrl = "https://discord.com/oauth2/authorize";
	private const string TokenUrl = "https://discord.com/api/oauth2/token";
	private const string ApiBaseUrl = "https://discord.com/api";
	private const string Scope = "identify guilds.members.read";
	private const int CallbackPort = 9999;
	private static readonly string CachePath = Path.Combine(AppContext.BaseDirectory, "discord_auth.cache");
	private static readonly byte[] CacheEntropy = Encoding.UTF8.GetBytes("FishNew.DiscordAuth.v1");

	private static readonly HttpClient Http = new HttpClient();

	public static bool EnsureAuthorized(string appName)
	{
		try
		{
			var cached = TryAuthorizeFromCacheAsync(CancellationToken.None).GetAwaiter().GetResult();
			if (cached is not null)
			{
				return true;
			}
		}
		catch
		{
			InvalidateCache();
		}

		using var form = new DiscordLoginForm(appName);
		return form.ShowDialog() == DialogResult.OK;
	}

	internal static async Task<DiscordAuthResult> AuthorizeAsync(Action<string>? status, CancellationToken cancellationToken)
	{
		var verifierBytes = RandomNumberGenerator.GetBytes(32);
		var codeVerifier = Base64UrlEncode(verifierBytes);
		var codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
		var state = Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
		var authorizeUrl = BuildAuthorizeUrl(codeChallenge, state);

		status?.Invoke("Waiting for browser callback...");
		using var callbackServer = new LoopbackCallbackServer(CallbackPort);
		var callbackTask = callbackServer.WaitForCallbackAsync(cancellationToken);

		status?.Invoke("Opening Discord...");
		if (!BrowserLauncher.TryOpen(authorizeUrl, out var launchError))
		{
			throw new InvalidOperationException("Unable to open the browser for Discord login: " + launchError);
		}

		status?.Invoke("Finish the login in your browser.");
		var callback = await callbackTask.ConfigureAwait(false);
		if (!string.IsNullOrWhiteSpace(callback.Error))
		{
			throw new InvalidOperationException("Discord login failed: " + callback.Error);
		}

		if (!string.Equals(callback.State, state, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Discord login state check failed.");
		}

		if (string.IsNullOrWhiteSpace(callback.Code))
		{
			throw new InvalidOperationException("Discord login did not return an authorization code.");
		}

		status?.Invoke("Exchanging authorization code...");
		var token = await ExchangeTokenAsync(callback.Code, codeVerifier, cancellationToken).ConfigureAwait(false);

		var authResult = await ValidateAccessTokenAsync(token.AccessToken, token.RefreshToken, token.ExpiresIn, status, cancellationToken).ConfigureAwait(false);
		SaveCache(authResult.Cache);
		return authResult.Result;
	}

	internal static DiscordWebhookSettings LoadWebhookSettings()
	{
		var cache = LoadCache();
		return new DiscordWebhookSettings
		{
			Enabled = cache?.NotifyIfStoppedEnabled ?? false,
			WebhookUrl = cache?.NotifyIfStoppedWebhookUrl ?? string.Empty,
		};
	}

	internal static void SaveWebhookSettings(bool enabled, string webhookUrl)
	{
		var cache = LoadCache() ?? new DiscordAuthCache();
		cache.NotifyIfStoppedEnabled = enabled;
		cache.NotifyIfStoppedWebhookUrl = webhookUrl ?? string.Empty;
		SaveCache(cache);
	}

	private static async Task<DiscordAuthOutcome> ValidateAccessTokenAsync(
		string accessToken,
		string refreshToken,
		int expiresIn,
		Action<string>? status,
		CancellationToken cancellationToken)
	{
		status?.Invoke("Checking Discord identity...");
		var user = await GetCurrentUserAsync(accessToken, cancellationToken).ConfigureAwait(false);

		status?.Invoke("Checking Discord server membership...");
		var member = await GetCurrentGuildMemberAsync(accessToken, RequiredGuildId, cancellationToken).ConfigureAwait(false);
		if (!member.RoleIds.Contains(RequiredRoleId, StringComparer.Ordinal))
		{
			throw new UnauthorizedAccessException("Access denied. You need the required role in the target Discord server.");
		}

		return new DiscordAuthOutcome(
			new DiscordAuthResult(user.Username, user.GlobalName, RequiredGuildId, RequiredRoleId),
			new DiscordAuthCache
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken,
				ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60)).UtcDateTime,
				UserName = user.Username,
				GlobalName = user.GlobalName ?? string.Empty,
				GuildId = RequiredGuildId,
				RoleId = RequiredRoleId,
			});
	}

	private static async Task<DiscordAuthResult?> TryAuthorizeFromCacheAsync(CancellationToken cancellationToken)
	{
		var cache = LoadCache();
		if (cache is null || string.IsNullOrWhiteSpace(cache.RefreshToken))
		{
			return null;
		}

		var token = await RefreshTokenAsync(cache.RefreshToken, cancellationToken).ConfigureAwait(false);
		var auth = await ValidateAccessTokenAsync(token.AccessToken, token.RefreshToken, token.ExpiresIn, null, cancellationToken).ConfigureAwait(false);
		SaveCache(auth.Cache);
		return auth.Result;
	}

	private static string BuildAuthorizeUrl(string codeChallenge, string state)
	{
		var query = new List<string>
		{
			"client_id=" + Uri.EscapeDataString(DiscordClientId),
			"response_type=code",
			"redirect_uri=" + Uri.EscapeDataString(RedirectUri),
			"scope=" + Uri.EscapeDataString(Scope),
			"state=" + Uri.EscapeDataString(state),
			"code_challenge_method=S256",
			"code_challenge=" + Uri.EscapeDataString(codeChallenge),
			"prompt=consent",
		};

		return AuthUrl + "?" + string.Join("&", query);
	}

	private static async Task<OAuthTokenResponse> ExchangeTokenAsync(string code, string codeVerifier, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
		{
			Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["client_id"] = DiscordClientId,
				["grant_type"] = "authorization_code",
				["code"] = code,
				["redirect_uri"] = RedirectUri,
				["code_verifier"] = codeVerifier,
			}),
		};

		using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("Discord token exchange failed: " + Compact(payload, 260));
		}

		var token = JsonSerializer.Deserialize<OAuthTokenResponse>(payload, JsonOptions());
		if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
		{
			throw new InvalidOperationException("Discord token exchange returned an empty access token.");
		}

		return token;
	}

	private static async Task<OAuthTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
		{
			Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["client_id"] = DiscordClientId,
				["grant_type"] = "refresh_token",
				["refresh_token"] = refreshToken,
			}),
		};

		using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("Discord token refresh failed: " + Compact(payload, 260));
		}

		var token = JsonSerializer.Deserialize<OAuthTokenResponse>(payload, JsonOptions());
		if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
		{
			throw new InvalidOperationException("Discord token refresh returned an empty access token.");
		}

		return token;
	}

	private static async Task<DiscordUser> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + "/users/@me");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException("Discord user lookup failed: " + Compact(payload, 260));
		}

		var user = JsonSerializer.Deserialize<DiscordUser>(payload, JsonOptions());
		if (user is null || string.IsNullOrWhiteSpace(user.Id))
		{
			throw new InvalidOperationException("Discord user lookup returned no identity.");
		}

		return user;
	}

	private static async Task<DiscordGuildMember> GetCurrentGuildMemberAsync(string accessToken, string guildId, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/users/@me/guilds/{guildId}/member");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			throw new UnauthorizedAccessException("Discord guild membership lookup failed: " + Compact(payload, 260));
		}

		var member = JsonSerializer.Deserialize<DiscordGuildMember>(payload, JsonOptions());
		if (member is null)
		{
			throw new InvalidOperationException("Discord guild member lookup returned no data.");
		}

		return member;
	}

	private static JsonSerializerOptions JsonOptions()
	{
		return new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		};
	}

	private static string Base64UrlEncode(byte[] data)
	{
		return Convert.ToBase64String(data)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}

	private static string Compact(string text, int max)
	{
		var s = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
		if (s.Length <= max)
		{
			return s;
		}

		return s.Substring(0, Math.Max(0, max - 3)) + "...";
	}

	private static DiscordAuthCache? LoadCache()
	{
		if (!File.Exists(CachePath))
		{
			return null;
		}

		try
		{
			var protectedBytes = File.ReadAllBytes(CachePath);
			var payload = ProtectedData.Unprotect(protectedBytes, CacheEntropy, DataProtectionScope.CurrentUser);
			return JsonSerializer.Deserialize<DiscordAuthCache>(payload, JsonOptions());
		}
		catch
		{
			return null;
		}
	}

	private static void SaveCache(DiscordAuthCache cache)
	{
		try
		{
			var existing = LoadCache();
			if (existing is not null)
			{
				cache.NotifyIfStoppedEnabled ??= existing.NotifyIfStoppedEnabled;
				if (string.IsNullOrWhiteSpace(cache.NotifyIfStoppedWebhookUrl))
				{
					cache.NotifyIfStoppedWebhookUrl = existing.NotifyIfStoppedWebhookUrl;
				}
			}

			var json = JsonSerializer.Serialize(cache, JsonOptions());
			var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), CacheEntropy, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(CachePath, protectedBytes);
		}
		catch
		{
		}
	}

	private static void InvalidateCache()
	{
		try
		{
			if (File.Exists(CachePath))
			{
				File.Delete(CachePath);
			}
		}
		catch
		{
		}
	}

	private sealed class LoopbackCallbackServer : IDisposable
	{
		private readonly TcpListener listener;

		public LoopbackCallbackServer(int port)
		{
			listener = new TcpListener(System.Net.IPAddress.Loopback, port);
			listener.Start();
		}

		public async Task<DiscordCallback> WaitForCallbackAsync(CancellationToken cancellationToken)
		{
			using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
			await using var stream = client.GetStream();
			var requestText = await ReadHttpRequestAsync(stream, cancellationToken).ConfigureAwait(false);
			var callback = ParseCallback(requestText);
			await WriteResponseAsync(stream, callback, cancellationToken).ConfigureAwait(false);
			return callback;
		}

		private static async Task<string> ReadHttpRequestAsync(Stream stream, CancellationToken cancellationToken)
		{
			var buffer = new byte[1024];
			var builder = new StringBuilder();

			while (true)
			{
				var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
				if (read <= 0)
				{
					break;
				}

				builder.Append(Encoding.ASCII.GetString(buffer, 0, read));
				if (builder.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
				{
					break;
				}

				if (builder.Length > 8192)
				{
					throw new InvalidOperationException("OAuth callback request was too large.");
				}
			}

			return builder.ToString();
		}

		private static DiscordCallback ParseCallback(string requestText)
		{
			var firstLineEnd = requestText.IndexOf("\r\n", StringComparison.Ordinal);
			if (firstLineEnd <= 0)
			{
				throw new InvalidOperationException("Discord callback request was malformed.");
			}

			var firstLine = requestText.Substring(0, firstLineEnd);
			var parts = firstLine.Split(' ');
			if (parts.Length < 2)
			{
				throw new InvalidOperationException("Discord callback request was malformed.");
			}

			var uri = new Uri("http://localhost" + parts[1]);
			var query = ParseQuery(uri.Query);
			return new DiscordCallback(uri.AbsolutePath, query);
		}

		private static Dictionary<string, string> ParseQuery(string query)
		{
			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(query))
			{
				return result;
			}

			foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
			{
				var split = part.Split('=', 2);
				var key = Uri.UnescapeDataString(split[0]);
				var value = split.Length > 1 ? Uri.UnescapeDataString(split[1].Replace('+', ' ')) : string.Empty;
				result[key] = value;
			}

			return result;
		}

		private static async Task WriteResponseAsync(Stream stream, DiscordCallback callback, CancellationToken cancellationToken)
		{
			var html = BuildResponseHtml(callback);
			var bytes = Encoding.UTF8.GetBytes(html);
			var header = Encoding.ASCII.GetBytes(
				"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n");
			await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
			await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
			await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		private static string BuildResponseHtml(DiscordCallback callback)
		{
			var title = callback.IsSuccess ? "Login complete" : "Login failed";
			var message = callback.IsSuccess
				? "You can close this tab and return to the app."
				: "Login failed. You can close this tab and try again.";

			return "<!doctype html><html><head><meta charset=\"utf-8\"><title>" + title + "</title></head>" +
			       "<body style=\"font-family:Segoe UI,Arial,sans-serif;padding:24px;\"><h2>" + title + "</h2><p>" + message + "</p></body></html>";
		}

		public void Dispose()
		{
			listener.Stop();
		}
	}
}

internal sealed class DiscordLoginForm : Form
{
	private readonly Label statusLabel;
	private readonly Button loginButton;
	private readonly Button cancelButton;
	private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
	private bool loginStarted;

	public DiscordLoginForm(string appName)
	{
		Text = appName + " Discord Login";
		Width = 420;
		Height = 180;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		StartPosition = FormStartPosition.CenterScreen;
		TopMost = true;
		BackColor = Color.FromArgb(24, 27, 31);
		ForeColor = Color.White;
		Font = new Font("Segoe UI", 9f);

		var panel = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(16),
			BackColor = Color.FromArgb(24, 27, 31),
		};

		var titleLabel = new Label
		{
			Text = "Open the browser login and finish the Discord check.",
			Left = 0,
			Top = 0,
			Width = 360,
			Height = 24,
			ForeColor = Color.White,
			BackColor = Color.FromArgb(24, 27, 31),
		};
		panel.Controls.Add(titleLabel);

		statusLabel = new Label
		{
			Text = "Ready",
			Left = 0,
			Top = 32,
			Width = 372,
			Height = 44,
			ForeColor = Color.FromArgb(148, 163, 184),
			BackColor = Color.FromArgb(24, 27, 31),
		};
		panel.Controls.Add(statusLabel);

		loginButton = new Button
		{
			Text = "Login",
			Left = 0,
			Top = 88,
			Width = 96,
			Height = 32,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(88, 101, 242),
			ForeColor = Color.White,
		};
		loginButton.FlatAppearance.BorderSize = 0;
		loginButton.Click += async (_, _) => await StartLoginAsync().ConfigureAwait(true);
		panel.Controls.Add(loginButton);

		cancelButton = new Button
		{
			Text = "Cancel",
			Left = 104,
			Top = 88,
			Width = 84,
			Height = 32,
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(55, 65, 81),
			ForeColor = Color.White,
		};
		cancelButton.FlatAppearance.BorderSize = 0;
		cancelButton.Click += (_, _) =>
		{
			cancellation.Cancel();
			DialogResult = DialogResult.Cancel;
			Close();
		};
		panel.Controls.Add(cancelButton);

		Controls.Add(panel);
		FormClosed += (_, _) => cancellation.Cancel();
		AcceptButton = loginButton;
		CancelButton = cancelButton;
	}

	private async Task StartLoginAsync()
	{
		if (loginStarted)
		{
			return;
		}

		loginStarted = true;
		loginButton.Enabled = false;
		cancelButton.Enabled = true;
		SetStatus("Opening browser...");

		try
		{
			var result = await DiscordRoleGate.AuthorizeAsync(SetStatus, cancellation.Token).ConfigureAwait(true);
			SetStatus("Authorized as " + result.DisplayName + ".");
			DialogResult = DialogResult.OK;
			Close();
		}
		catch (OperationCanceledException)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
		catch (Exception ex)
		{
			SetStatus(ex.Message);
			MessageBox.Show(this, ex.Message, "Discord Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			loginStarted = false;
			loginButton.Enabled = true;
		}
	}

	private void SetStatus(string text)
	{
		if (IsDisposed)
		{
			return;
		}

		if (InvokeRequired)
		{
			BeginInvoke(new Action(() => SetStatus(text)));
			return;
		}

		statusLabel.Text = text;
	}
}

internal sealed class DiscordAuthResult
{
	public string DisplayName { get; }
	public string UserName { get; }
	public string GlobalName { get; }
	public string GuildId { get; }
	public string RoleId { get; }

	public DiscordAuthResult(string userName, string globalName, string guildId, string roleId)
	{
		UserName = userName;
		GlobalName = globalName;
		GuildId = guildId;
		RoleId = roleId;
		DisplayName = !string.IsNullOrWhiteSpace(globalName) ? globalName : userName;
	}
}

internal sealed class DiscordCallback
{
	public string Path { get; }
	public Dictionary<string, string> Query { get; }
	public string Code { get; }
	public string State { get; }
	public string Error { get; }
	public bool IsSuccess => string.Equals(Path, "/callback", StringComparison.OrdinalIgnoreCase) &&
	                         string.IsNullOrWhiteSpace(Error);

	public DiscordCallback(string path, Dictionary<string, string> query)
	{
		Path = path;
		Query = query;
		Code = query.TryGetValue("code", out var code) ? code : string.Empty;
		State = query.TryGetValue("state", out var state) ? state : string.Empty;
		Error = query.TryGetValue("error", out var error) ? error : string.Empty;
	}
}

internal sealed class OAuthTokenResponse
{
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; } = string.Empty;
	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; } = string.Empty;
	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; } = 3600;
}

internal sealed class DiscordUser
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;
	[JsonPropertyName("username")]
	public string Username { get; set; } = string.Empty;
	[JsonPropertyName("global_name")]
	public string? GlobalName { get; set; }
}

internal sealed class DiscordGuildMember
{
	[JsonPropertyName("roles")]
	public List<string> RoleIds { get; set; } = [];
}

internal sealed class DiscordAuthCache
{
	[JsonPropertyName("access_token")]
	public string AccessToken { get; set; } = string.Empty;
	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; } = string.Empty;
	[JsonPropertyName("expires_at_utc")]
	public DateTime ExpiresAtUtc { get; set; }
	[JsonPropertyName("user_name")]
	public string UserName { get; set; } = string.Empty;
	[JsonPropertyName("global_name")]
	public string GlobalName { get; set; } = string.Empty;
	[JsonPropertyName("guild_id")]
	public string GuildId { get; set; } = string.Empty;
	[JsonPropertyName("role_id")]
	public string RoleId { get; set; } = string.Empty;
	[JsonPropertyName("notify_if_stopped_enabled")]
	public bool? NotifyIfStoppedEnabled { get; set; }
	[JsonPropertyName("notify_if_stopped_webhook_url")]
	public string NotifyIfStoppedWebhookUrl { get; set; } = string.Empty;
}

internal sealed class DiscordWebhookSettings
{
	public bool Enabled { get; set; }
	public string WebhookUrl { get; set; } = string.Empty;
}

internal sealed record DiscordAuthOutcome(DiscordAuthResult Result, DiscordAuthCache Cache);
