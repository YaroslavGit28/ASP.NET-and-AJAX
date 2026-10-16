
# TodoManagementApi — настройка OpenAPI (Swagger) в ASP.NET Core

## Цель работы

Изучить настройку OpenAPI (Swagger) в ASP.NET Core и научиться задавать информацию об API через `OpenApiInfo`.

## Задание

Реализовать проект **Todo Management API** и настроить Swagger со следующими данными:

- **Title**: `"Todo Management API"`
- **Version**: `"v1"`
- **Description**: `"API для управления задачами. Поддерживает фильтрацию и создание."`
- **Contact**: `Name = "API Support"`, `Email = "support@example.com"`
- **License**: `Name = "MIT"`, `Url = "https://opensource.org/licenses/MIT"`

Дополнительно реализованы эндпоинты:

- `GET /api/todos` — получение списка задач;
- `GET /api/todos?completed=true` — фильтрация задач по статусу;
- `GET /api/todos/{id}` — получение задачи по идентификатору;
- `POST /api/todos` — создание новой задачи.

## Структура проекта

```text
TodoManagementApi/
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── TodoManagementApi.csproj
└── TodoManagementApi.http
```

## Запуск проекта

Открой терминал в папке, где находится `TodoManagementApi.csproj`, и выполни:

```powershell
dotnet restore
dotnet build
dotnet run --urls http://localhost:5000
```

После запуска приложение будет доступно по адресам:

- API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`
- OpenAPI-документ: `http://localhost:5000/swagger/v1/swagger.json`

## Скриншоты

### Скриншот 1 — запуск проекта в командной строке

На скриншоте должен быть виден терминал с выполненной командой `dotnet run --urls http://localhost:5000` и сообщением:

```text
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```

![Рисунок 1 — Запуск проекта](screenshots/01-run.png)

*Рисунок 1 — Запуск проекта.*

### Скриншот 2 — страница Swagger UI

Открой в браузере `http://localhost:5000/swagger`. На скриншоте должны быть видны:

- заголовок **Todo Management API**;
- версия **v1**;
- описание **"API для управления задачами. Поддерживает фильтрацию и создание."**;
- список эндпоинтов `GET /api/todos`, `GET /api/todos/{id}`, `POST /api/todos`.

![Рисунок 2 — Swagger UI](screenshots/02-swagger-ui.png)

*Рисунок 2 — Настроенная документация Swagger.*

### Скриншот 3 — проверка OpenAPI-документа через командную строку

Выполни во втором терминале:

```powershell
curl.exe -i http://localhost:5000/swagger/v1/swagger.json
```

На скриншоте должны быть видны `200 OK` и блок `info` с полями `title`, `version`, `description`, `contact`, `license`.

![Рисунок 3 — swagger.json](screenshots/03-swagger-json.png)

*Рисунок 3 — Проверка OpenAPI-документа через командную строку.*

## Вывод

В ходе работы был создан проект **TodoManagementApi** на ASP.NET Core Minimal API. Подключён и настроен Swagger с использованием `OpenApiInfo`: заданы Title, Version, Description, Contact и License. Реализованы эндпоинты для получения, фильтрации и создания задач. Работа подтверждена скриншотами запуска, Swagger UI и ответа `swagger.json`.
