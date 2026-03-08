# 🎮 GameStore

A full-stack game storefront built with **ASP.NET Core Minimal API** + **Angular**.

## 🧰 Tools & Frameworks

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Minimal%20API-512BD4?logo=dotnet&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-10-6E4C13?logo=nuget&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-20-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.8-3178C6?logo=typescript&logoColor=white)
![RxJS](https://img.shields.io/badge/RxJS-7.8-B7178C?logo=reactivex&logoColor=white)
![SCSS](https://img.shields.io/badge/SCSS-Styling-CC6699?logo=sass&logoColor=white)
![Node.js](https://img.shields.io/badge/Node.js-20+-339933?logo=nodedotjs&logoColor=white)

## ✨ Features

- 🕹️ Browse a classic games catalog
- 🔎 Search by game name or genre
- 🖼️ Show cover image for each game
- 🧾 Fallback generated cover when no image is found
- 🛒 Cart popup with quantity and total
- 💜 Wishlist popup
- 🔌 REST API for games, genres, and image upload

## 📁 Project Structure

```text
GameStore/
├── GameStore.Api/   # ASP.NET Core API + EF Core + SQLite
└── GameStore.Web/   # Angular frontend
```

## ✅ Prerequisites

- .NET SDK 10.x
- Node.js 20+
- npm

## 🚀 Run Locally

1. Start API:

```bash
cd GameStore.Api
dotnet restore
dotnet run
```

2. Start frontend (new terminal):

```bash
cd GameStore.Web
npm install
npm start
```

3. Open:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5059`

Frontend proxy:

- `/api/*` → `http://localhost:5059/*`
- `/uploads/*` → `http://localhost:5059/uploads/*`

## 📡 API Endpoints

### Games

- `GET /games`
- `GET /games/{id}`
- `GET /games/{id}/cover`
- `POST /games`
- `PUT /games/{id}`
- `DELETE /games/{id}`
- `POST /games/{id}/image` (multipart/form-data)

### Genres

- `GET /genres`

## 📚 What I Learned From This Project

- Building a complete full-stack app with **ASP.NET Core Minimal APIs** and **Angular standalone components**
- Designing and consuming REST APIs using **HttpClient** and **RxJS**
- Managing data with **EF Core + SQLite** and automatic seeding/migrations
- Serving static assets (game covers) from the backend and displaying them in Angular
- Creating responsive modern UI with **SCSS**, reusable states, and popup interactions
- Structuring a project for local development with API proxying in Angular

