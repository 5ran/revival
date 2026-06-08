using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

internal static class SupabaseLicenseGate
{
    // Hardcoded global gate config.
    // You can also override URL/anon key with env vars if needed:
    //   GAMETHING_SUPABASE_URL
    //   GAMETHING_SUPABASE_ANON_KEY
    private const string DefaultSupabaseUrl = "https://ekybgvjmunbrdskvmfgx.supabase.co/rest/v1/";
    private const string DefaultAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVreWJndmptdW5icmRza3ZtZmd4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3Njg4NjE2NTMsImV4cCI6MjA4NDQzNzY1M30.GIA6G2AOiMQ8_rX7NGtIwt2AGbwUVbuOAg5b5j-_pWY";
    private const string DefaultGateTable = "app_gate";
    private const string HardcodedGlobalKey = "MY-GLOBAL-KEY-V1";
    private static string lastGateError = string.Empty;

    public static bool EnsureAuthorized(string appName)
    {
        string url = (Environment.GetEnvironmentVariable("GAMETHING_SUPABASE_URL") ?? DefaultSupabaseUrl).Trim();
        string anon = (Environment.GetEnvironmentVariable("GAMETHING_SUPABASE_ANON_KEY") ?? DefaultAnonKey).Trim();
        string table = (Environment.GetEnvironmentVariable("GAMETHING_SUPABASE_TABLE") ?? DefaultGateTable).Trim();

        if (url.IndexOf("YOUR_PROJECT", StringComparison.OrdinalIgnoreCase) >= 0
            || anon.IndexOf("YOUR_SUPABASE_ANON_KEY", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            MessageBox.Show(
                appName + ": license server is not configured.\nSet Supabase URL and anon key in SupabaseLicenseGate.cs or environment variables.",
                "License Setup Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        if (VerifyGlobalGate(url, anon, table, HardcodedGlobalKey))
            return true;

        MessageBox.Show(
            appName + ": access denied (global key revoked or mismatched).\n\n" + lastGateError,
            "Access Denied",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        return false;
    }

    private static bool VerifyGlobalGate(string baseUrl, string anonKey, string table, string expectedKey)
    {
        lastGateError = string.Empty;
        try
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
            string safeBase = baseUrl.TrimEnd('/');
            if (safeBase.EndsWith("/rest/v1", StringComparison.OrdinalIgnoreCase))
                safeBase = safeBase.Substring(0, safeBase.Length - "/rest/v1".Length);
            string endpoint = safeBase + "/rest/v1/" + table
                + "?select=app_key,active&id=eq.1&limit=1";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
            req.Method = "GET";
            req.Timeout = 8000;
            req.Headers["apikey"] = anonKey;
            req.Headers["Authorization"] = "Bearer " + anonKey;
            req.Accept = "application/json";

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string json = sr.ReadToEnd().Trim();
                if (json == "[]")
                {
                    lastGateError = "Gate query returned empty row. endpoint=" + endpoint;
                    return false;
                }
                bool active = json.IndexOf("\"active\":true", StringComparison.OrdinalIgnoreCase) >= 0;
                bool keyMatch = json.IndexOf("\"app_key\":\"" + expectedKey.Replace("\"", "\\\"") + "\"", StringComparison.Ordinal) >= 0;
                if (!active || !keyMatch)
                    lastGateError = "Gate row mismatch. response=" + Compact(json, 220);
                return active && keyMatch;
            }
        }
        catch (WebException wex)
        {
            string detail = wex.Message;
            try
            {
                HttpWebResponse resp = wex.Response as HttpWebResponse;
                if (resp != null)
                {
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string body = sr.ReadToEnd();
                        detail = "HTTP " + (int)resp.StatusCode + " " + resp.StatusCode + " | " + Compact(body, 220);
                    }
                }
            }
            catch
            {
            }
            lastGateError = detail;
            return false;
        }
        catch
        {
            lastGateError = "Unknown gate error";
            return false;
        }
    }

    private static string Compact(string text, int max)
    {
        string s = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        if (s.Length <= max) return s;
        return s.Substring(0, Math.Max(0, max - 3)) + "...";
    }

}
