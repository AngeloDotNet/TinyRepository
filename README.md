# Tiny Repository for .NET EF Core
[![NuGet Version](https://img.shields.io/nuget/v/TinyRepository.svg?style=flat-square)](https://www.nuget.org/packages/TinyRepository/)

A lightweight generic repository for Entity Framework Core
<!--
This library provides a generic EF Core-based repository with core CRUD (asynchronous) methods.
-->

## 🏷️ Introduction

A lightweight generic repository pattern implementation for Entity Framework Core, designed to simplify data access and management in .NET applications.

<!--
This NuGet package provides a simple implementation of the Repository and Unit of Work pattern using Entity Framework Core. It provides a generic interface, IRepository<T, TKey>, with common methods for asynchronous CRUD operations, as well as a concrete implementation, EfRepository<T, TKey>, that uses an EF Core DbContext.
-->

## 🛠️ Installation

### Prerequisites

- .NET 8.0 SDK (latest version)

### Setup

The library is available on [NuGet](https://www.nuget.org/packages/TinyRepository), just search for _Identity.Module.API_ in the Package Manager GUI or run the following command in the .NET CLI:

```shell
dotnet add package TinyRepository
```

> [!TIP]
> See the [documentation]() for a list of helpful examples.

<!--
## 💡 Release Notes

Release notes are available [here]().
-->

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⭐ Give a Star

Don't forget that if you find this project helpful, please give it a ⭐ on GitHub to show your support and help others discover it.

## 🤝 Contributing

The project is constantly evolving. Contributions are always welcome. Feel free to report issues and submit pull requests to the repository, following the steps below:

1. Fork the repository
2. Create a feature branch (starting from the develop branch)
3. Make your changes
4. Submit a pull requests (targeting develop)

<!--
Note:
- L'implementazione non chiama SaveChanges internamente su Add/Update/Remove: il commit è responsabilità dell'unit-of-work.
- Puoi estendere `EfRepository` aggiungendo metodi per paging, projection, includi (Include), ecc.
- Se preferisci metodi sincroni, puoi aggiungerli ma in app moderne è preferibile usare gli async.

- Le operazioni Add/Update/Remove/Range non chiamano SaveChanges internamente; è responsabilità dell'UnitOfWork/Service chiamante effettuare il commit.
- GetPagedAsync senza orderBy non garantisce ordine deterministico; è consigliato passare un orderBy per paginazione affidabile.

- L'ordinamento dinamico usa expressions costruite a runtime; se passi un nome di proprietà non valido verrà lanciata un'eccezione. Per sicurezza, validare i nomi prima in scenari esposti all'utente.
- Quando usi paginazione fornisci un ordine deterministico (orderBy o orderByProperty) per risultati coerenti tra pagine.
-->