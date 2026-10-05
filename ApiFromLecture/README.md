# ApiFromLecture

Учебный ASP.NET Core Web API проект с пары.

Реализованы Model Binding (Route/Query/Body/Header), кастомный binder CSV-массива, DTO, DataAnnotations, собственная валидация, FluentValidation, ProblemDetails, глобальный IExceptionHandler и Swagger.

Запуск:
```bash
dotnet restore
dotnet run
```
Swagger: `http://localhost:5080/swagger`

Примеры: `GET /api/products/1`, `GET /api/products/by-ids?ids=1,2,3`, `POST /api/products`.
