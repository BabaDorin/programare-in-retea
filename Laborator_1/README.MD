# Dorin Baba
## TI-211 FR
# --------------------------------------------------------------
### Chat C# instructions

**1. Configurează IP/Port:**
* **Server:** Modifică `ipAddress` și `port` în `ChatServer.cs` (linia ~16).
* **Client:** Modifică `serverIp` și `port` în `ChatClient.cs` (linia ~10) să corespundă cu serverul.
    * Pentru test local, `127.0.0.1` e OK. Pentru rețea, folosește IP-ul real al serverului.

**2. Pornește Serverul:**
* **Compilează (o singură dată):**
    * Navighează în folderul serverului și rulează: `dotnet build`
* **Rulează:**
    * Din folderul serverului: `dotnet run`
    * Sau rulează `.exe`-ul din `bin/Debug/netX.X/`

**3. Pornește Clientul/Clienții:**
* **Compilează (o singură dată per client, dacă nu e deja făcut):**
    * Navighează în folderul clientului și rulează: `dotnet build`
* **Rulează (pentru fiecare client dorit):**
    * Din folderul clientului (într-un nou terminal): `dotnet run`
    * Sau rulează `.exe`-ul din `bin/Debug/netX.X/`
* Serverul acceptă maxim **5** clienți simultan.