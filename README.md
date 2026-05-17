# ZPassFit Backend

## Технологии

-   **ASP.NET Core**
-   **Entity Framework Core** — ORM для работы с базой данных
-   **PostgreSQL** — основная база данных
-   **gRPC** — для взаимодействия с ИИ-сервисом
-   **YooKassa** — интеграция с платежной системой
-   **BenchmarkDotNet** — для нагрузочного тестирования и бенчмарков

## Установка и запуск

### Предварительные требования

-   .NET 10.0 SDK
-   Docker и Docker Compose

### Настройка

1.  Создайте файл `.env` на основе `.env.example`:
    ```bash
    cp .env.example .env
    ```
2.  Заполните необходимые переменные в `.env`, особенно данные для ЮKassa, если планируете использовать платежи.

### Запуск инфраструктуры

Для запуска базы данных и других зависимостей используйте Docker Compose:
```bash
docker compose up -d
```

### Запуск приложения

Из корневой директории бэкенда:
```bash
dotnet run --project ZPassFit
```

## Дополнительные команды

-   `make docker-build` — сборка Docker-образа бэкенда.
-   `make benchmark` — запуск бенчмарков производительности.
-   `make kiota-yookassa` — генерация клиента для API ЮKassa с помощью Kiota.

## Структура проекта

-   `ZPassFit/` — основной проект API.
-   `ZPassFit.Test/` — юнит-тесты.
-   `ZPassFit.IntegrationTest/` — интеграционные тесты.
-   `ZPassFit.Benchmarks/` — тесты производительности.
-   `ZPassFit.YooKassa/` — клиент для интеграции с ЮKassa.
