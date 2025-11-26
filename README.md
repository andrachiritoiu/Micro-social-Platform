#📱 Micro-Social Platform
O aplicație web de tip rețea de socializare dezvoltată în ASP.NET Core MVC, care permite utilizatorilor să creeze profiluri, să posteze conținut multimedia, să interacționeze prin grupuri și să urmărească activitatea altor persoane.



## Funcționalități Principale
### 1. Gestionarea Utilizatorilor și Profiluri
Autentificare și Roluri: Sistem bazat pe ASP.NET Identity.


- Vizitator: Poate căuta utilizatori și vizualiza profiluri publice.


- Utilizator Înregistrat: Acces complet la funcționalități (postări, grupuri, follow).


- Administrator: Poate modera conținutul și gestiona utilizatorii/grupurile.


- Profiluri: Editare nume, descriere și poză de profil. Posibilitatea de a seta profilul ca Public sau Privat.


- Căutare: Căutare utilizatori după nume sau părți din nume.

### 2. Interacțiune Socială (Follow & Feed)
- Sistem Follow: Cereri de urmărire unidirecționale (similar Instagram). Pentru profilurile private, cererile necesită aprobare.


- Feed Personalizat: Afișează postările persoanelor urmărite, ordonate descrescător după dată.

3. Grupuri și Comunități
Utilizatorii pot crea grupuri și devin automat moderatori.

Accesul în grupuri se face pe bază de cerere ("Join") acceptată de moderator.

Discuții și mesaje în cadrul grupului.

4. Conținut Multimedia

- Postări: Suport pentru text, imagini și videoclipuri.

-Reacții: Sistem de like-uri și comentarii la postări.

- Validare ca un utilizator să nu poată da like de două ori la aceeași postare.

### Integrare AI - Filtrare Conținut
Aplicația include un "Companion AI" pentru moderarea automată a conținutului, conform cerințelor.

Funcționalitate: Înainte ca o postare sau un comentariu să fie salvat în baza de date, textul este trimis către un serviciu AI.

Logică:

1.Utilizatorul apasă "Post".

2.Controller-ul interceptează cererea și extrage textul.

3.Textul este analizat pentru hate speech, insulte sau limbaj discriminatoriu.

4.Dacă AI-ul returnează un flag negativ, postarea este blocată, iar utilizatorul primește un mesaj de eroare prietenos.

5.Dacă textul este curat, postarea este salvată.

### Tehnologii Utilizate

-Framework: ASP.NET Core MVC 9.0 

-Limbaj: C#

-Baza de date: Microsoft SQL Server

-ORM: Entity Framework Core

-Autentificare: ASP.NET Core Identity

-Frontend: HTML5, CSS3, Bootstrap (pentru design responsive) 

-AI Service: 

📂 Structura Proiectului
-Proiectul respectă arhitectura MVC (Model-View-Controller):

-Models: Definește structura bazei de date (Users, Groups, Posts, Comments).

-Views: Interfața cu utilizatorul (Razor Pages).

-Controllers: Gestionează logica aplicației și apelurile către baza de date și serviciul AI.
