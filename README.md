# Projekt BYROO - ChatApp

Jednoduchá chatovacia aplikácia s real-time komunikáciou. Server bežiaci na ASP.NET spracováva REST požiadavky a WebSocket spojenia cez SignalR, WPF klient slúži ako desktopové rozhranie a MySQL databáza uchováva používateľov, miestnosti a históriu správ.

Celá aplikácia funguje lokálne - klient aj server bežia na tom istom stroji (alebo v rámci lokálnej siete), žiadny externý hosting nie je potrebný.

---

### Obsah
1. [Technologický stack](#1-technologický-stack)
2. [Štruktúra projektu](#2-štruktúra-projektu)
3. [Ako to spustiť](#3-ako-to-spustiť)
4. [REST API endpointy](#4-rest-api-endpointy)
5. [SignalR Hub](#5-signalr-hub)
6. [Databáza](#6-databáza)
7. [Rozhodnutia pri riešení](#7-rozhodnutia-pri-riešení)

---

## 1. Technologický stack

| Vrstva | Technológia |
|---|---|
| Server | ASP.NET (minimálne API + controllery) |
| Real-time | SignalR (WebSocket) |
| Klient | WPF (.NET) |
| Databáza | MySQL + Entity Framework Core |
| Logovanie | Serilog |
| MVVM toolkit | CommunityToolkit.Mvvm |

---

## 2. Štruktúra projektu

Riešenie je rozdelené do štyroch projektov, aby bola jasná hranica medzi serverom, klientom, dátovou vrstvou a zdieľanými modelmi.

```
src/
├── ChatApp.Server/          # ASP.NET server - REST API + SignalR hub
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── RoomsController.cs
│   │   └── MessagesController.cs
│   ├── Hubs/
│   │   └── ChatHub.cs
│   └── Program.cs
│
├── ChatApp.Client/          # WPF desktopový klient
│   ├── Views/
│   │   ├── LoginView.xaml
│   │   └── MainView.xaml
│   ├── ViewModels/
│   │   ├── LoginViewModel.cs
│   │   └── MainViewModel.cs
│   ├── Services/
│   │   ├── IChatService.cs
│   │   └── ChatService.cs
│   └── Converters/
│
├── ChatApp.Data/            # EF Core DbContext, entity, migrácie
│   ├── Entities/
│   ├── Configurations/
│   └── Migrations/
│
└── ChatApp.Shared/          # DTO modely zdieľané medzi serverom a klientom
    └── DTOs/
```

---

## 3. Ako to spustiť

### Predpoklady

- .NET 10 SDK
- MySQL server (lokálne bežiaci, napr. XAMPP alebo samostatná inštalácia)

### Krok za krokom

**1. Vytvorenie databázy**

V MySQL si treba vytvoriť databázu a používateľa (alebo použiť root):

```sql
CREATE DATABASE chatapp;
CREATE USER 'chatapp'@'localhost' IDENTIFIED BY 'chatapp_dev';
GRANT ALL PRIVILEGES ON chatapp.* TO 'chatapp'@'localhost';
```

**2. Konfigurácia (.env)**

Skopírujte `.env.example` do `.env` v koreňovom adresári a upravte hodnoty podľa vášho prostredia:

```bash
cp .env.example .env
```

Obsah `.env`:

```
ConnectionStrings__DefaultConnection=Server=localhost;Database=chatapp;User=chatapp;Password=chatapp_dev
ServerUrl=http://localhost:5050
```

Connection string a URL servera nie sú uložené priamo v kóde - načítajú sa z environment premenných. Súbor `.env` je v `.gitignore`, takže sa nedostane do repozitára.

Alternatívne môžete vytvoriť `src/ChatApp.Server/appsettings.Development.json`:

```json
{
  "Urls": "http://0.0.0.0:5050",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=chatapp;User=chatapp;Password=chatapp_dev"
  }
}
```

Aj tento súbor je v `.gitignore`.

**3. Migrácie**

```bash
dotnet ef database update --project src/ChatApp.Data --startup-project src/ChatApp.Server
```

Toto vytvorí všetky tabuľky (Users, Rooms, Messages, RoomMembers).

**4. Spustenie servera**

```bash
dotnet run --project src/ChatApp.Server
```

Server štartuje na porte, ktorý je nastavený v `launchSettings.json` (štandardne `http://localhost:5050`).

**5. Spustenie klienta**

```bash
dotnet run --project src/ChatApp.Client
```

URL servera, na ktorý sa klient pripája, sa berie z environment premennej `ServerUrl`. Ak nie je nastavená, použije sa fallback `http://localhost:5050` z `appsettings.json`. Ak server beží na inom porte alebo IP, stačí zmeniť hodnotu v `.env`.

**6. Testovanie**

Pre vyskúšanie chatu medzi dvoma používateľmi stačí spustiť klienta dvakrát (dva terminály alebo dve inštancie). Každý sa prihlási pod iným menom, obaja sa pripoja do rovnakej miestnosti a správy sa zobrazujú v reálnom čase.

---

## 4. REST API endpointy

Server poskytuje tri controllery pre základné operácie:

| Metóda | Endpoint | Popis |
|---|---|---|
| POST | `/api/auth/login` | Prihlásenie (alebo registrácia) podľa username |
| GET | `/api/rooms` | Zoznam všetkých miestností |
| POST | `/api/rooms` | Vytvorenie novej miestnosti |
| GET | `/api/rooms/{roomId}/messages?page=1&pageSize=50` | Načítanie histórie správ (stránkovanie) |

Login funguje jednoducho - pošle sa username, ak používateľ s týmto menom neexistuje, vytvorí sa nový. Žiadne heslá, keďže cieľom bolo demonštrovať real-time chat, nie autentifikáciu.

---

## 5. SignalR Hub

WebSocket komunikácia prebieha cez `ChatHub` na endpoint `/chat`. Hub poskytuje tri metódy:

- **JoinRoom(roomId, userId)** - pripojí sa do miestnosti (pridá sa aj do DB ak tam ešte nie je) a notifikuje ostatných
- **LeaveRoom(roomId, userId)** - odpojí sa z miestnosti
- **SendMessage(roomId, userId, content)** - odošle správu, uloží ju do databázy a broadcastne ju všetkým v miestnosti

Klient dostáva tieto udalosti:
- `ReceiveMessage` - nová správa
- `UserJoined` - niekto sa pripojil do miestnosti
- `UserLeft` - niekto odišiel

Hub používa `IDbContextFactory<ChatDbContext>` namiesto priameho injectovania DbContextu, pretože SignalR huby majú transient lifecycle a DbContext je scoped - priame injektovanie by spôsobovalo problémy pri viacerých súčasných spojeniach.

---

## 6. Databáza

Databázový model obsahuje štyri entity:

- **User** - id, username, dátum vytvorenia
- **Room** - id, názov (unikátny, max 100 znakov), dátum vytvorenia
- **Message** - id, obsah (max 2000 znakov), odosielateľ, miestnosť, čas odoslania
- **RoomMember** - väzobná tabuľka medzi používateľom a miestnosťou (M:N)

EF Core konfigurácie sú v `ChatApp.Data/Configurations/` a definujú indexy (napr. index na `SentAt` pre rýchle načítanie histórie) a vzťahy medzi entitami.

---

## 7. Rozhodnutia pri riešení

**Prečo SignalR a nie čistý WebSocket?**
SignalR je nadstavba nad WebSocket, ktorá rieši reconnect, fallback na long-polling ak WebSocket nie je dostupný, a groupovanie spojení. Pre chat aplikáciu je to ideálne, lebo netreba manuálne riešiť odpájanie a opätovné pripájanie klientov.

**Prečo MVVM?**
WPF je na MVVM postavený - data binding, commands, notifikácie. Použil som CommunityToolkit.Mvvm, ktorý generuje boilerplate cez source generátory (atribúty ako `[ObservableProperty]` a `[RelayCommand]`), takže ViewModely sú prehľadné.

**Prečo oddelený Data projekt?**
Entity, DbContext a migrácie sú v samostatnom projekte `ChatApp.Data`, aby sa dátová vrstva dala prípadne použiť aj inde a nebola priamo zviazaná so serverom.

**Prečo Shared projekt?**
DTO modely (ako `MessageDto`, `RoomDto` atď.) používa aj server aj klient. Namiesto ich duplikovania sú v `ChatApp.Shared`, na ktorý majú oba projekty referenciu.

**Vizuálne rozlíšenie správ**
Vlastné správy sa zobrazujú so zeleným pozadím a zarovnané doprava, cudzie správy sú šedé a zarovnané doľava - podobne ako v bežných chatovacích aplikáciách.
