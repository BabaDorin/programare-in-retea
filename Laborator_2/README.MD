# Baba Dorin
## TI-211 FR
### Laborator 2 (UDP) - Aplicație Chat C# prin UDP
# --------------------------------------------------------------

### 1. Configurare Server și Client (Opțional)
* **Server (`UdpChatServer.cs`):**
    * IP-ul (`host`) este setat la `"0.0.0.0"` (ascultă pe toate interfețele).
    * Portul (`port`) este setat la `12345`. Modifică dacă e necesar (linia ~14).
* **Client (`UdpChatClient.cs`):**
    * IP-ul serverului (`serverHost`) este setat la `"127.0.0.1"`. Schimbă-l cu IP-ul real al serverului dacă rulează pe altă mașină (linia ~13).
    * Portul serverului (`serverPort`) este setat la `12345`. Trebuie să corespundă cu cel al serverului (linia ~14).

### 2. Compilare și Pornire Server
* **Compilează (o singură dată):**
    * Navighează în folderul serverului și rulează: `dotnet build`
* **Rulează:**
    * Din folderul serverului: `dotnet run`
    * Sau rulează `.exe`-ul din `bin/Debug/netX.X/`

Serverul va afișa pe ce IP și port ascultă. Notează adresele IP și porturile clienților pe măsură ce se conectează, așa cum sunt afișate de server (ex: `New client added: 127.0.0.1:XXXXX`). Acestea sunt necesare pentru mesajele private.

### 3. Compilare și Pornire Client(ți)
* **Compilează (o singură dată per client, dacă nu e deja făcut):**
    * Navighează în folderul clientului și rulează: `dotnet build`
* **Rulează (pentru fiecare client dorit):**
    * Din folderul clientului (într-un nou terminal): `dotnet run`
    * Sau rulează `.exe`-ul din `bin/Debug/netX.X/`

Fiecare client va afișa adresa sa locală (IP:Port) la pornire.

### 4. Opțiuni Mesaje
1.  **Mesaje Publice:** Scrie pur și simplu mesajul și apasă Enter. Va fi trimis tuturor clienților conectați.
2.  **Mesaje Private (`/pm`):**
    * Folosește formatul: `/pm <ip_destinatar>:<port_destinatar> <mesajul_tău>`
    * `<ip_destinatar>:<port_destinatar>` sunt adresa și portul clientului căruia vrei să-i trimiți mesajul. Aceste detalii sunt afișate de server când un client se conectează (ex: `New client added: 127.0.0.1:54321`) sau pot fi afișate de client la pornire. Serverul în Python folosea `127.0.0.1` ca IP static pentru clienți și un port alocat dinamic; în exemplul C#, clientul își afișează IP-ul și portul local.
3.  **Ieșire (`exit`):** Scrie `exit` și apasă Enter pentru a părăsi chat-ul.