# Сервис доставки и самовывоза

## Описание сервиса

Сервис - система телеграм-ботов для доставки и самовывоза товаров. Инфраструктура ботов построена абстрактно, чтобы можно было создать любой бот под различные задачи.

Боты: админский бот, клиентский бот.

## Стэк
- .NET 8
- База данных: MS SQL Server
- ORM: Entity Framework
- Брокер сообщений: RabbitMq
- Логгер: NLog

## Технологии
- Использование брокера сообщений
- Использование ORM
- паттерн консистенции бд и брокера сообщений Outbox Messages
- Dependency Injection
- Управление правами пользователей

## Админский бот

### Фукнционал
- Управление каталогом
- Управление правами администраторов
- Управление заказами
- Личный кабинет администратора

## Клиентский бот

### Фукнционал
- Просмотр каталога
- Управление корзиной
- Создание и просмотр заказов
