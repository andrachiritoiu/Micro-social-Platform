# Micro-social Platform

O aplicație web de tip rețea de socializare dezvoltată în **ASP.NET Core MVC**, creată pentru a facilita interacțiunea între utilizatori prin postări, grupuri și un sistem de urmărire (follow).

---

## Funcționalități Principale

### 👤 Gestionarea Utilizatorilor
* **Tipuri de utilizatori:** Vizitator, Utilizator Înregistrat, Administrator.
* **Autentificare:** Sistem complet de Login/Register folosind **ASP.NET Identity**.
* **Profiluri:** Posibilitatea de a seta profilul ca **Public** sau **Privat**. Utilizatorii își pot edita numele, descrierea și poza de profil.
* **Căutare:** Găsirea utilizatorilor după nume sau părți din nume.

### 👥 Grupuri și Comunități
* Crearea de grupuri noi (creatorul devine moderator).
* Sistem de **Join Request** (membrii trebuie acceptați de moderator).
* Discuții în cadrul grupurilor.

### 📝 Postări și Feed
* Creare postări cu conținut multimedia (Text, Foto, Video).
* Feed personalizat care afișează postările persoanelor urmărite (Followings).
* Sistem de Like-uri și Comentarii.

### Componentă AI (Content Moderation)
* Integrare cu un serviciu AI pentru filtrarea automată a conținutului neadecvat (hate speech, insulte).
* Verificarea se face înainte de salvarea în baza de date.

---

## Tehnologii Utilizate

* **Framework:** .NET 9.0 (ASP.NET Core MVC)
* **Limbaj:** C#
* **Bază de date:** SQL Server (prin Entity Framework Core)
* **Frontend:** HTML5, CSS3, Bootstrap
* **Versiune Control:** Git & GitHub

---

## 📂 Structura Proiectului

```text
Micro-social-Platform/
├── Controllers/       # Logica de business (PostsController, GroupsController etc.)
├── Models/            # Entitățile bazei de date (ApplicationUser, Post, Group etc.)
├── Views/             # Interfața utilizator (Razor Pages)
├── Data/              # Contextul EF Core și Migrațiile
├── Services/          # Logica pentru AI Content Moderation
├── wwwroot/           # Resurse statice (CSS, JS, Imagini uploadate)
└── Dockerfile         # Configurare containerizare
```

## Instalare și Rulare (Comenzi)

Urmează pașii de mai jos pentru a rula proiectul pe mașina locală.

<<<<<<< HEAD
### 1️. Clonează repository-ul
```bash
git clone https://github.com/andrachiritoiu/Micro-social-Platform.git
=======
### 1. Clonează repository-ul
Deschide terminalul (Command Prompt, PowerShell sau Git Bash) și rulează comanda:
```bash
git clone [https://github.com/andrachiritoiu/Micro-social-Platform.git](https://github.com/andrachiritoiu/Micro-social-Platform.git)
>>>>>>> efb3eb4a47a9c6afe9b76812eaceb1b9c58010d0
```

### 2. Navighează în folderul proiectului
```bash
cd Micro-social-Platform
```

<<<<<<< HEAD
### 3. Aplică migrațiile bazei de date

Asigură-te că SQL Server este pornit și connection string-ul este configurat corect.
=======
### 3. Configurează Baza de Date și AI
Înainte de a rula migrațiile, deschide fișierul `appsettings.json` și fă următoarele setări:

1.  Verifică dacă **Connection String-ul** este corect pentru SQL Server-ul tău.
2.  Adaugă cheia API pentru serviciul de moderare AI (necesară pentru validarea postărilor):
    ```json
    "AISettings": {
      "ApiKey": "CHEIA_TA"
    }
    ```

După ce ai salvat fișierul, rulează următoarele comenzi în terminal (sau Package Manager Console) pentru a crea baza de date și a popula tabelele cu **Seed Data**:
>>>>>>> efb3eb4a47a9c6afe9b76812eaceb1b9c58010d0

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

<<<<<<< HEAD
### 4. Rulează aplicația
=======


### 4. Rulează Aplicația
Pornește serverul local cu următoarea comandă:

>>>>>>> efb3eb4a47a9c6afe9b76812eaceb1b9c58010d0
```bash
dotnet run
```

Aplicația va fi accesibilă în browser la adresa: `https://localhost:` 

---


### 🔑 Conturi de Test (Seed Data)
După rularea migrațiilor, baza de date este populată automat cu următorii utilizatori pentru testare rapidă:

* **Administrator:** `admin@test.com` / Parola: `Admin1!`
* **Utilizator 1:** `user1@test.com` / Parola: `User1!`
* **Utilizator 2:** `user2@test.com` / Parola: `User2!`
  

<<<<<<< HEAD
Baza de date include următoarele entități principale:
- **Users**
- **Posts**
- **Comments**
- **Reactions**
- **Follows**
- **Groups**
- **GroupMembers**
- **Messages**
- **Notifications**

Relațiile sunt gestionate prin **Entity Framework Core**, folosind convenții și configurări explicite unde a fost necesar.

---

## Roadmap / Task-uri

### Done (MVP)
- [x] Autentificare & roluri (ASP.NET Identity)
- [x] Profil public/privat + editare profil
- [x] Postare (text + media) + feed
- [x] Like-uri și comentarii
- [ ] Follow + feed filtrat după following
- [x] Grupuri + join request + moderare de bază
- [X] Notificări UI complete (follow, like, comment, join accept)
- [X] Mesagerie: inbox + status citit + paginare
- [X] Search îmbunătățit (filtre)

### In progress / Next
- [ ] Moderare AI înainte de salvarea postărilor
- [ ] UI : empty states, skeleton loading, toast messages

### Nice to have
- [ ] Refresh feed fără reload (AJAX)
- [ ] Pagini publice pentru grupuri + reguli și descriere extinsă
- [ ] Admin dashboard (rapoarte, moderare content, user management)
- [ ] Teste unitare și de integrare (Controllers & Services)
- [ ] CI pipeline (GitHub Actions) + badge în README




=======
## 🐳 Rulare cu Docker (Alternativ)

Dacă preferi să nu instalezi SQL Server local, poți rula aplicația folosind Docker.

### Cerințe preliminare
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalat și pornit.

### Pași pentru rulare

1.  **Deschide terminalul** în rădăcina proiectului (unde se află fișierul `docker-compose.yml`).
2.  **Construiește și pornește containerele:**
    ```bash
    docker-compose up --build
    ```
3.  **Accesarea aplicației:**
    După ce log-urile indică faptul că aplicația a pornit, deschide browserul la:
    * **http://localhost:8080**

4.  **Oprirea aplicației:**
    Pentru a opri serviciile, apasă `Ctrl+C` sau rulează:
    ```bash
    docker-compose down
    ```

---

>>>>>>> efb3eb4a47a9c6afe9b76812eaceb1b9c58010d0
