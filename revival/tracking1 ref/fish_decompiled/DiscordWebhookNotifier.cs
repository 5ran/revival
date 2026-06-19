using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

internal static class DiscordWebhookNotifier
{
	private static readonly HttpClient Http = new HttpClient();

	public static Task SendTestAsync(string webhookUrl, CancellationToken cancellationToken)
	{
		return SendAsync(webhookUrl, "Test from Fish New", cancellationToken);
	}

	public static Task SendStoppedAsync(string webhookUrl, string message, CancellationToken cancellationToken)
	{
		return SendAsync(webhookUrl, message, cancellationToken);
	}

	private static async Task SendAsync(string webhookUrl, string content, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(webhookUrl))
		{
			throw new InvalidOperationException("Webhook URL is empty.");
		}

		if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhookUri))
		{
			throw new InvalidOperationException("Webhook URL is not valid.");
		}

		var payload = JsonSerializer.Serialize(new DiscordWebhookPayload
		{
			Content = content,
		});

		using var request = new HttpRequestMessage(HttpMethod.Post, webhookUri)
		{
			Content = new StringContent(payload, Encoding.UTF8, "application/json"),
		};

		using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			throw new InvalidOperationException("Discord webhook send failed: " + Compact(body, 240));
		}
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

	private sealed class DiscordWebhookPayload
	{
		[JsonPropertyName("content")]
		public string Content { get; set; } = string.Empty;
	}
}
