using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Security.Principal;
using CommandLine;

namespace DatabaseMigrator.Cmd
{

    class Options
    {
        [Option('d', "database", Required = true, HelpText = "Database connection string")]
        public string DatabaseConnection { get; set; }

        [Option('s', "schema", Default = "Jobbr", HelpText = "Jobbr Schema")]
        public string Schema { get; set; }

        [Option('a', "artefactdir", Required = true, HelpText = "Path where artefacts are stored")]
        public string ArtefactDirectory { get; set; }

        [Option('r', "rundir", Required = false, HelpText = "Path where run folder where created, keep emptry")]
        public string RunDirectory { get; set; }

        [Option(Default = false, HelpText = "Apply changes to database and folder")]
        public bool Apply { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(RunOptionsAndReturnExitCode);
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            Console.WriteLine("Jobbr Migrator");
            Console.WriteLine("==============");
            Console.WriteLine("\n");

            if (ValidateConfiguration(opts)) return;

            PrintConfiguration(opts);

            Execute(opts);
        }

        private static void Execute(Options opts)
        {
            
        }

        private static void PrintConfiguration(Options opts)
        {
            Console.WriteLine();
            Console.Write("".PadRight(Console.WindowWidth, '-'));

            Console.WriteLine($" Database:    {opts.DatabaseConnection}");
            Console.WriteLine($" ArtefactDir: {ShortenPath(opts.ArtefactDirectory, Console.WindowWidth - 15)}");

            if (!string.IsNullOrWhiteSpace(opts.RunDirectory))
            {
                Console.WriteLine($" RunDir:      {ShortenPath(opts.RunDirectory, Console.WindowWidth - 15)}");
            }

            Console.WriteLine($" Save Change: {opts.Apply}");

            Console.Write("".PadRight(Console.WindowWidth, '-'));
        }

        private static bool ValidateConfiguration(Options opts)
        {
            Console.WriteLine("Validating Configuration");

            if (!ValidateDatabase(opts)) return true;

            if (!ValidateTables(opts)) return true;

            if (!ValidateArtefactDir("ArtefactDir: ", opts.ArtefactDirectory)) return true;
            if (!ValidateArtefactDir("RunDir:      ", opts.ArtefactDirectory)) return true;
            return false;
        }

        private static bool ValidateArtefactDir(string name, string path)
        {
            Console.Write($" - {name}");

            var fullPath = Path.GetFullPath(path);

            if (Directory.Exists(fullPath))
            {
                Console.WriteLine($"Directory found. Folders: {Directory.GetDirectories(fullPath).Length}");
                return true;
            }

            Console.WriteLine( "Not found.");

            return false;
        }

        private static bool ValidateDatabase(Options opts)
        {
            try
            {
                Console.Write(" - Database:   ");
                var newCon = new SqlConnection();
                newCon.ConnectionString = opts.DatabaseConnection;
                newCon.Open();

                if (newCon.State == ConnectionState.Open)
                {
                    Console.WriteLine($" Connected to {newCon.DataSource} with database '{newCon.Database}'");
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed. {e.Message}");
            }

            return false;
        }

        private static bool ValidateTables(Options opts)
        {
            try
            {
                Console.Write(" - Tables:      ");
                var newCon = new SqlConnection();
                newCon.ConnectionString = opts.DatabaseConnection;
                newCon.Open();

                var cmd = new SqlCommand($"SELECT t.name, s.name FROM sys.Tables AS t LEFT JOIN sys.schemas AS s ON s.schema_id = t.schema_id WHERE s.name = '{opts.Schema}'", newCon);

                var reader = cmd.ExecuteReader();

                var tables = new List<string>();

                while (reader.Read())
                {
                    tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
                }

                if (tables.Count == 3)
                {
                    Console.WriteLine(string.Join(", ", tables.ToArray()));
                    return true;
                }
                else
                {
                    Console.WriteLine("Missing!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed. {e.Message}");
            }

            return false;
        }

        private static string ShortenPath(string path, int maxLength)
        {
            int pathLength = path.Length;

            string[] parts;
            parts = path.Split('\\');

            int startIndex = (parts.Length - 1) / 2;
            int index = startIndex;

            String output = "";
            output = String.Join("\\", parts, 0, parts.Length);

            decimal step = 0;
            int lean = 1;

            while (output.Length >= maxLength && index != 0 && index != -1)
            {
                parts[index] = "...";

                output = String.Join("\\", parts, 0, parts.Length);

                step = step + 0.5M;
                lean = lean * -1;

                index = startIndex + ((int)step * lean);
            }

            return output;
        }
    }
}
