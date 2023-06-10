﻿namespace Migrondi.Core

/// DU that represents the currently supported drivers
[<RequireQualifiedAccess>]
type MigrondiDriver =
  | Mssql
  | Sqlite
  | Postgresql
  | Mysql

/// Represents the configuration that will be used to run migrations
type MigrondiConfig = {
  /// An ADO compatible connection string
  /// which will be used to connect to the database
  connection: string
  /// A relative path like string to the directory where migration files are stored
  migrations: string
  /// The name of the table that will be used to store migration information
  tableName: string
  /// a string that represents the drivers that can be used
  /// mysql | postgres | mssql | sqlite
  driver: MigrondiDriver
}

/// Represents a migration stored in the database
type MigrationRecord = {
  id: int64
  name: string
  timestamp: int64
}

/// Object that represents an SQL migration file on disk
type Migration = {
  name: string
  timestamp: int64
  /// the actual SQL statements that will be used to run against the database
  upContent: string
  /// the actual SQL statements that will be used to run against the database
  /// when rolling back migrations from the database
  downContent: string
}

/// Migration information can be obtained from a file or the database
/// this DU allows to identify where the information is coming from
[<RequireQualifiedAccess>]
type MigrationSource =
  | SourceCode of Migration
  | Database of MigrationRecord

[<RequireQualifiedAccess>]
type MigrationType =
  | Up
  | Down

  member this.AsString =
    match this with
    | Up -> "UP"
    | Down -> "DOWN"

// Migrondi.Core
// Migrations.fs - Operate on migration
// - create migration
// - read migration statement content
// - list migrations statements
// Database.fs - relates to the create, read, find, and delete migrations from the database
// - apply migrations
// - rollback migrations
// - list migrations
// - find migration
// - find last migration
// Api.fs - expose the public api to make this a cohesive library
// - Up
// - Down
// - List
// - Status

// Migrondi.CLI
// Arguments.fs - CLI arguments
// Commands.fs - CLI commands
// Middleware.fs - CLI middleware
// Program.fs - CLI entry point