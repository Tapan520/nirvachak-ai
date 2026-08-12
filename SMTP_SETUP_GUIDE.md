# Email Setup Guide for Forgot Password Feature

## What Was Configured

The forgot password email uses HTTP-based APIs (no SMTP).
Both **Resend** and **Mailjet** are supported. The first one configured is used automatically.

Files involved:
- `Infrastructure/Services/EmailService.cs` — dual-provider email service
- `Program.cs` — HTTP clients registered for both providers

---

## Provider Priority

1. **Resend** — used if `Resend__ApiKey` is set
2. **Mailjet** — used if `Mailjet__ApiKey` and `Mailjet__SecretKey` are both set
3. Neither configured — emails are skipped (a warning is logged)

---

## Production Deployment (Railway)

Set environment variables in your Railway service. Only one provider needs to be configured.

### Option 1: Resend (Recommended — Free 3,000 emails/month)

Sign up at: https://resend.com, create an API Key, and verify your sender domain.

    Resend__ApiKey   = re_xxxxxxxxxxxxxxxxxxxx
    Resend__From     = noreply@yourdomain.com
    Resend__FromName = Nirvachak AI
    AppBaseUrl       = https://your-production-domain.com

> For quick testing without a verified domain, use `onboarding@resend.dev` as the From address.

### Option 2: Mailjet (Free 200 emails/day)

Sign up at: https://mailjet.com, get API Key + Secret Key from Settings -> API Keys.

    Mailjet__ApiKey    = your-mailjet-api-key
    Mailjet__SecretKey = your-mailjet-secret-key
    Mailjet__From      = tapchauhan2001@gmail.com
    Mailjet__FromName  = Nirvachak AI
    AppBaseUrl         = https://your-production-domain.com

Both providers use HTTPS (port 443) — they work on Railway where all SMTP ports are blocked.

---

## Log Messages

No provider configured:

    [Email] No email provider configured (Resend or Mailjet). Skipping email to user@example.com

Resend success:

    [Email][Resend] Sent 'Reset Your Password - Nirvachak AI' to user@example.com

Mailjet success:

    [Email][Mailjet] Sent 'Reset Your Password - Nirvachak AI' to user@example.com

Failed:

    [Email][Resend] Failed to send 'Reset Your Password' to user@example.com

---

## Troubleshooting

- **Resend 403**: Domain not verified — use `onboarding@resend.dev` as the From address for testing
- **Mailjet 401**: Invalid API key or secret — double-check Railway env vars
- **Email in spam**: Add SPF/DKIM DNS records for your domain (both providers have guides)

---

## Security Notes

- NEVER commit API keys to Git
- Use Railway environment variables for all credentials
- Rotate API keys periodically from your provider dashboard
