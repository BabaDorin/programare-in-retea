# Dorin Baba TI-211 FR
## Email sender using SMTP

This C# console app directly interacts with SMTP, IMAP, and POP3 protocols using built-in .NET networking.

**Warning:** This application bypasses SSL/TLS certificate validation for simplicity during development. **DO NOT USE IN PRODUCTION.**

## Setup

1.  Clone the project.
2.  Ensure .NET SDK is installed.

## Gmail App Password

For Gmail accounts, you **must** use an [App password](https://myaccount.google.com/security) instead of your regular account password, especially if you have 2-Step Verification enabled[cite: 1].

### How to get a Gmail App Password:
1.  Go to your Google Account's Security page: [myaccount.google.com/security](https://myaccount.google.com/security)[cite: 1].
2.  Under "How you sign in to Google," ensure "2-Step Verification" is **On**. If not, enable it[cite: 1].
3.  Click on "App passwords"[cite: 1].
4.  Follow the prompts to select "Mail" as the app and "Other (Custom name)" as the device[cite: 1].
5.  Click "Generate" and copy the 16-character password displayed[cite: 1]. This is the password you'll use in the application[cite: 1].

## How to Run

Navigate to the project's root directory in your terminal.

### Send Email

**Syntax:**
`dotnet run send <from_email> <to_email> <subject> <body> <filename_or_NONE> <password>`

* `<from_email>`: The sender's email address.
* `<to_email>`: The recipient's email address.
* `<subject>`: The email subject (enclose in quotes if it contains spaces).
* `<body>`: The email body (enclose in quotes if it contains spaces).
* `<filename_or_NONE>`: Path to an attachment file (e.g., `un_fisier.txt`). Use `NONE` if no attachment.
* `<password>`: Your Gmail App Password.

**Examples:**

* **Send without attachment:**
    ```bash
    dotnet run send "your@gmail.com" "to@example.com" "Hello from C# App" "This is a test email sent from my low-level C# app." NONE "your_app_password"
    ```

* **Send with attachment:**
    *(Ensure `un_fisier.txt` is in the same directory or provide its full path.)*
    ```bash
    dotnet run send "your@gmail.com" "to@example.com" "C# App with Attachment" "Check out the attached file." un_fisier.txt "your_app_password"
    ```

### Fetch Emails

**Syntax:**
`dotnet run fetch <email_user> <email_password> [-p <protocol>] [-f <folder>] [-s <search_criteria>]`

* `<email_user>`: Your email address.
* `<email_password>`: Your Gmail App Password.
* `-p` or `--protocol`: (Optional) `IMAP` (default) or `POP3`.
* `-f` or `--folder`: (Optional, IMAP only) The folder to fetch from (e.g., `INBOX`, `Sent`, `Spam`). Default is `INBOX`.
* `-s` or `--search`: (Optional, IMAP only) Search criteria (e.g., `ALL` (default), `UNSEEN`, `"FROM 'sender@example.com'"`). Note: Search parsing is basic.

**Examples:**

* **IMAP (all inbox):**
    ```bash
    dotnet run fetch "your@gmail.com" "app_password"
    ```

* **IMAP (specific sender):**
    ```bash
    dotnet run fetch "your@gmail.com" "app_password" -p IMAP -f INBOX -s "FROM 'no-reply@google.com'"
    ```

* **POP3:**
    ```bash
    dotnet run fetch "your@gmail.com" "app_password" -p POP3
    ```

for testing purpose

dotnet run send "bvd.dorin@gmail.com" "dev.dorin.baba@gmail.com" "C# App with Attachment" "See attached file for more info." un_fisier.txt "confidential"