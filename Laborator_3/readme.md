# Dorin Baba
### TI-211 FR
### Laborator 3 - Aplicație client DNS în C#
### ----------------------------------------------------------------------------------------------------------------------------

## Prerechizite:
Această aplicație folosește biblioteca `DnsClient.NET` pentru funcționalități avansate de interogare DNS, inclusiv specificarea unui server DNS custom.
Pentru a compila și rula codul, trebuie să adăugați pachetul NuGet `DnsClient` la proiectul vostru C#.
Puteți face asta rulând următoarea comandă în directorul proiectului:
`dotnet add package DnsClient`

## Pentru a rula aplicația, trebuie să furnizați argumente în linia de comandă:
1.  `-u <ip_dns>`: (Opțional) Specifică un server DNS customizat pentru interogări.
2.  `-d <domeniu>`: Rezolvă un nume de domeniu la adrese IP (interogare tip 'A').
3.  `-i <ip>`: Rezolvă o adresă IP la numele de domeniu asociat (reverse DNS, interogare tip 'PTR').

Este posibil să combinați opțiunea `-u` cu `-d` sau `-i`.
Dacă sunt specificate atât `-d <domeniu>` cât și `-i <ip>`, aplicația va prioritiza rezolvarea domeniului (`-d`).

### Exemple de comenzi pentru testare (presupunând că executabilul compilat se numește `DnsResolverApp.exe`):

* **Setare DNS custom și rezolvare domeniu:**
    `DnsResolverApp.exe -u 8.8.8.8 -d google.com`

* **Rezolvare domeniu folosind DNS-ul sistemului:**
    `DnsResolverApp.exe -d example.com`

* **Setare DNS custom și rezolvare IP (reverse DNS):**
    `DnsResolverApp.exe -u 8.8.8.8 -i 142.250.180.14` (IP pentru google.com)

* **Rezolvare IP (reverse DNS) folosind DNS-ul sistemului (și DnsClient dacă e specificat un server custom):**
    `DnsResolverApp.exe -i 142.250.180.14`

* **Prioritatea argumentelor:** Dacă se specifică și `-d` și `-i`, `-d` va fi procesat:
    `DnsResolverApp.exe -u 8.8.8.8 -i 172.217.18.14 -d microsoft.com`
    *(În acest caz, `microsoft.com` va fi rezolvat)*

### Teste pentru gestionarea erorilor:

* **IP invalid pentru reverse lookup:**
    `DnsResolverApp.exe -i 1.2.3.999`

* **Domeniu invalid/inexistent:**
    `DnsResolverApp.exe -d numedomeniuinvalidcarenuareipsigur.com`

* **Server DNS invalid/inaccesibil:**
    `DnsResolverApp.exe -u 1.2.3.999 -d google.com` (Aplicația va afișa o eroare pentru IP-ul DNS invalid și ar putea folosi DNS-ul sistemului sau eșua, depinzând de implementarea exactă a gestionării erorii pentru `SetDnsServer`)
    `DnsResolverApp.exe -u 256.0.0.1 -d google.com` (IP invalid)

### Compilare și Rulare:
1.  **Salvați codul:** Salvați codul C# de mai sus într-un fișier numit `DnsResolverApp.cs`.
2.  **Creați un proiect (recomandat):**
    * `dotnet new console -o MyDnsResolver`
    * `cd MyDnsResolver`
    * Înlocuiți conținutul fișierului `Program.cs` generat cu codul `DnsResolverApp.cs`.
    * Adăugați pachetul DnsClient: `dotnet add package DnsClient`
3.  **Compilează:**
    * `dotnet build`
4.  **Rulează:**
    * Navigați în `bin/Debug/netX.X/` (unde `netX.X` e versiunea .NET, ex. `net8.0`)
    * Rulați executabilul cu argumentele dorite: `MyDnsResolver.exe -d google.com`
    * Sau din directorul proiectului: `dotnet run -- -d google.com` (notați `--` pentru a separa argumentele `dotnet run` de cele ale aplicației).