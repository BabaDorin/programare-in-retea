# Dorin Baba - TI-211 FR
### Utilizare HTTP Client pentru comunicare dintre Shop API Client si UTM Shop.

### Instructiuni de lansare:
- Lanseaza UTM Shop API. Asigura-te ca adresa API-ului este https://localhost:5001; 
- Build & Run ShopApiClient.exe
- Odata lansata, utilizeaza linia de comanda pentru a indica operatiunile pe care sa le indeplineasca ShopApiClient.exe. Mai jos este prezentat meniul:
    - -m GET
    - -m GET -i 1"
    - -m GET -i 1 -d products
    - -m POST -d "New Category Title"
    - -m POST -i 1 -d '{\"title\":\"New Product\",\"price\":9.99}'
    - -m PUT -i 1 -d "Updated Category Title"
    - -m DELETE -i 1