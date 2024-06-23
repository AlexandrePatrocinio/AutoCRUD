## AutoCRUD

*Português (pt-br)

Uma extensão simples para criar automaticamente um CRUD para projetos .NET usando minimal APIs no .NET 8.

Ele é usado para criar rapidamente endpoints para operações de criação, leitura, atualização e exclusão de uma entidade específica por meio dos métodos de extensão das classes base WebApplicationBuilder e WebApplication. Estas extensões estão disponiveis no namespace AutoCRUD.Extensions. 

Também permite integrar validações personalizadas para cada endpoint usando a interface IServiceAutoCRUDValidation disponível no namespace AutoCRUD.Services.

Inicialmente ela possui integração com o postgresSQL et SQLServer através de duas implementações genéricas da interface IRepository disponíveis nos namespaces AutoCRUD.Data.NpgSql et AutoCRUD.Data.SqlClient respectivamente. Para isso, AutoCRUD utilisa o pacote Dapper.

Para saber como usá-lo favor verificar o repositório de exemplo chamado "api" em meu github.

#Projeto
Esta extensão foi inspirada no desafio que ocorreu no Brasil em 2023 chamado rinhabackend (https://github.com/zanfranceschi/rinha-de-backend-2023-q3)
Usei a ideia para gerar de maneira facil uma api que implementa um CRUD automaticamente (dai o  nome) com os endpoints solicitados no desafio.

*Français (fr-fr)

Une extension simple pour créer automatiquement un CRUD pour les projets .NET en utilisant un minimal API dans .NET 8.

Il est utilisé pour créer rapidement des points de terminaison pour les opérations de création, de lecture, de mise à jour et de suppression pour une entité spécifique via les méthodes d'extension des classes de base WebApplicationBuilder et WebApplication. Ces extensions sont disponibles dans l'espace de noms AutoCRUD.Extensions.

Il vous permet également d'intégrer des validations personnalisées pour chaque point de terminaison à l'aide de l'interface IServiceAutoCRUDValidation disponible dans l'espace de noms AutoCRUD.Services.

Initialement, il est intégré à PostgresSQL et SQLServer via deux implémentations génériques de l'interface IRepository disponibles dans les espaces de noms AutoCRUD.Data.NpgSql et AutoCRUD.Data.SqlClient respectivement. Pour cela, AutoCRUD utilise le package Dapper.

Pour savoir comment l'utiliser, veuillez consulter l'exemple de référentiel appelé "api" sur mon github.

#Projet
Cet extension a été inspirée par le défi qui a eu lieu au Brésil en 2023 appelé rinhabackend (https://github.com/zanfranceschi/rinha-de-backend-2023-q3).
J’ai utilisé l’idée pour générer facilement une API qui implémente un CRUD automatiquement (d’où le nom) avec les points de terminaison demandés dans le défi.

*English (en-us)

A simple extension to automatically create a CRUD for .NET projects using a minimal API in .NET 8.

It is used to quickly create endpoints for create, read, update, and delete operations for a specific entity via the extension methods of the WebApplicationBuilder and WebApplication base classes. These extensions are available in the AutoCRUD.Extensions namespace.

It also allows you to integrate custom validations for each endpoint using the IServiceAutoCRUDValidation interface available in the AutoCRUD.Services namespace.

Initially, it is integrated with PostgresSQL and SQLServer via two generic implementations of the IRepository interface available in the AutoCRUD.Data.NpgSql and AutoCRUD.Data.SqlClient namespaces respectively. For this, AutoCRUD uses the Dapper package.

To learn how to use it, please see the example repository called "api" on my github.

#Project
This extension was inspired by the challenge that occurred in Brazil in 2023 called rinhabackend (https://github.com/zanfranceschi/rinha-de-backend-2023-q3).
I used the idea to easily generate an API that implements a CRUD automatically (hence the name) with the endpoints requested in the challenge.


*Package dependencies
- Dapper Version="2.1.35"
- Npgsql Version="8.0.2"
- System.Data.SqlClient Version="4.8.6
