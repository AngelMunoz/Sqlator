namespace Sqlator

open System
open System.IO
open System.Data.SQLite
open FSharp.Data.Dapper
open Types
open System.Text.RegularExpressions


module Migrations =
    let inline private (=>) (column: string) (value: 'T) = column, box value
    let private versionPattern = Regex @"(V|v)[0-9]{1,}"

    let private getMigrations() =
        let path = Path.Combine(".", "migrations")
        let dir = DirectoryInfo(path)

        dir.EnumerateFiles()
        |> Seq.toArray
        |> Array.Parallel.map (fun file ->
            let name = file.Name
            let reader = file.OpenText()
            let content = reader.ReadToEnd()
            let splitname = name.Split('.')
            let filenameerr = sprintf "file: %s does not comply with the \"V[number].[Name].sql\" nomenclature" name

            let version =
                let v =
                    if splitname.Length = 3 then splitname.[0] else failwith filenameerr
                if versionPattern.IsMatch v then v.[1] else failwith filenameerr

            { name = name
              version =
                  version
                  |> Char.GetNumericValue
                  |> int
              content = content })
        |> Array.sortBy (fun m -> m.version)

    let private getConnection() = SqliteConnection(new SQLiteConnection("Data Source=Sqlator.db"))

    let private getLastMigration() =
        Async.Catch
            (querySeqAsync<Migration> (getConnection)
                 { script "Select * FROM Migrations ORDER BY Date DESC LIMIT 1;" })

    let private vMigration (migration: MigrationFile) =
        let migrationContent =
            let insertStmn = "INSERT INTO Migrations (Name, Version, Date) VALUES(@Name, @Version, @Date);"
            sprintf "BEGIN TRANSACTION;\n%s\n%s\nEND TRANSACTION;" migration.content insertStmn


        querySingleAsync<int> getConnection {
            script migrationContent
            parameters
                (dict
                    [ "Name" => migration.name
                      "Version" => migration.version
                      "Date" => DateTimeOffset.Now.ToUnixTimeMilliseconds() ])
        }


    let private runMigrations (list: array<MigrationFile>) =
        match Array.isEmpty list with
        | false -> list |> Array.map vMigration
        | true -> Array.empty

    let asyncRunMigrations() =
        async {
            let! result = getLastMigration()
            let migration =
                match result with
                | Choice1Of2 migration ->
                    printfn "Migrations found"
                    migration |> Seq.tryHead
                | Choice2Of2 ex ->
                    printfn "Migrations table not found, starting from scratch"
                    None

            let migrations = getMigrations()

            let migrationsToRun =
                match migration with
                | None -> runMigrations migrations
                | Some migration ->
                    let index = migrations |> Array.findIndex (fun m -> m.name = migration.Name)
                    match index + 1 = migrations.Length with
                    | true -> Array.empty
                    | false ->
                        let pending = migrations |> Array.skip (index + 1)
                        printfn "Running pending %i migrations" pending.Length
                        runMigrations pending
            printfn "Will run %i migrations" migrationsToRun.Length
            return! migrationsToRun |> Async.Sequential
        }
        |> Async.RunSynchronously
