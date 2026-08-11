# SMTP Email Setup Guide for Forgot Password Feature

## ? What Was Configured

The forgot password email functionality is now ready to use. The following files were updated:

1. **`appsettings.json`** - Added SMTP section (for production deployment)
2. **`appsettings.Development.json`** - Added SMTP config for local development
3. **`Infrastructure/Services/EmailService.cs`** - Updated to read from both JSON and environment variables

---

## ?? Quick Start (Local Development)

### Option 1: Gmail (Recommended for Testing)

1. **Enable 2-Step Verification** on your Google account:
   - Go to: https://myaccount.google.com/security

2. **Create an App Password**:
   - Go to: https://myaccount.google.com/apppasswords
   - Generate a new app password for "Mail"
   - Copy the 16-character password (no spaces)

3. **Edit `appsettings.Development.json`**:
   ```json
   "Smtp": {
     "Host": "smtp.gmail.com",
     "Port": "587",
     "User": "your-actual-email@gmail.com",
     "Pass": "abcd efgh ijkl mnop",  // Your 16-char app password (remove spaces)
     "From": "your-actual-email@gmail.com",
     "FromName": "Nirvachak AI"
   }
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Test the feature**:
   - Navigate to: https://localhost:7237/Account/Login
   - Click "Forgot Password?"
   - Enter a valid email address from your test database
   - Check your inbox (and spam folder)

---

### Option 2: Outlook/Hotmail

```json
"Smtp": {
  "Host": "smtp-mail.outlook.com",
  "Port": "587",
  "User": "your-email@outlook.com",
  "Pass": "your-password",
  "From": "your-email@outlook.com",
  "FromName": "Nirvachak AI"
}
```

---

### Option 3: SendGrid (Professional - Free 100 emails/day)

1. Sign up at: https://sendgrid.com/
2. Create an API Key
3. Verify a sender email address

```json
"Smtp": {
  "Host": "smtp.sendgrid.net",
  "Port": "587",
  "User": "apikey",
  "Pass": "SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "From": "verified-sender@yourdomain.com",
  "FromName": "Nirvachak AI"
}
```

---

### Option 4: Brevo (formerly Sendinblue - Free 300 emails/day)

1. Sign up at: https://www.brevo.com/
2. Get SMTP credentials from Settings ? SMTP & API
3. Verify a sender email

```json
"Smtp": {
  "Host": "smtp-relay.brevo.com",
  "Port": "587",
  "User": "your-brevo-login-email@example.com",
  "Pass": "your-brevo-smtp-key",
  "From": "verified-sender@yourdomain.com",
  "FromName": "Nirvachak AI"
}
```

---

## ?? Production Deployment (Railway/Docker)

For production, **DO NOT** put credentials in `appsettings.json`. Use environment variables instead:

### Railway Environment Variables
```
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-production-email@gmail.com
SMTP_PASS=your-app-password
SMTP_FROM=your-production-email@gmail.com
SMTP_FROM_NAME=Nirvachak AI
AppBaseUrl=https://your-production-domain.com
```

### Docker Environment Variables
```bash
docker run -e SMTP_HOST=smtp.gmail.com \
           -e SMTP_PORT=587 \
           -e SMTP_USER=your-email@gmail.com \
           -e SMTP_PASS=your-app-password \
           -e SMTP_FROM=your-email@gmail.com \
           -e AppBaseUrl=https://yourdomain.com \
           your-image-name
```

---

## ?? Troubleshooting

### Email not sending?

1. **Check the logs**:
   ```
   [Email] SMTP not configured. Skipping email...
   ```
   ? Fill in all required fields in `appsettings.Development.json`

2. **Gmail "Less secure app" error**:
   ? Use App Password, NOT your regular Gmail password

3. **Connection timeout**:
   - Verify your firewall allows port 587
   - Try port 465 with SSL
   - Check if your ISP blocks SMTP

4. **Email goes to spam**:
   - Use a verified sender domain (SendGrid/Brevo)
   - Add SPF/DKIM records to your domain

### How to verify it's working?

Check the console logs when you submit the forgot password form:

? **Success**:
```
[Email] Sent 'Reset Your Password - Nirvachak AI' to user@example.com
```

? **Not configured**:
```
[Email] SMTP not configured. Skipping email to user@example.com
```

? **Failed**:
```
[Email] Failed to send 'Reset Your Password' to user@example.com
System.Net.Mail.SmtpException: ...
```

---

## ?? Security Notes

- ?? **NEVER** commit `appsettings.Development.json` with real credentials to Git
- ?? Add `appsettings.Development.json` to `.gitignore` if not already present
- ? Use environment variables for production deployments
- ? Gmail App Passwords are safer than using your main password
- ? Rotate SMTP credentials periodically

---

## ?? Email Template

The password reset email contains:
- A secure reset link valid for a limited time
- Instructions on how to reset the password
- A warning not to share the link
- Support contact information

The link format: `https://localhost:7237/Account/ResetPassword?email=user@example.com&token=...`

---

## ? Build Status

**Build succeeded!** ?  
The project compiles successfully with the SMTP configuration in place.

```
Build succeeded with 4 warning(s) in 75.2s
```

*Warnings are pre-existing and unrelated to SMTP changes.*
