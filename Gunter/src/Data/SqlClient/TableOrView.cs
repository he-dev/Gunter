﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gunter.Annotations;
using Gunter.Data.Attachements;
using Gunter.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;

namespace Gunter.Data.SqlClient
{
    [PublicAPI]
    public class TableOrView : IDataSource
    {
        private readonly Program _program;
        private readonly Factory _factory;

        public delegate TableOrView Factory();

        //[JsonConstructor]
        public TableOrView(ILogger<TableOrView> logger, Program program, Factory factory)
        {
            _program = program;
            _factory = factory;
            Logger = logger;
        }

        private ILogger Logger { get; }

        public int Id { get; set; }

        public string Merge { get; set; }

        [NotNull]
        [Mergable]
        //[JsonRequired]
        public string ConnectionString { get; set; }

        [NotNull]
        [Mergable]
        //[JsonRequired]
        public string Query { get; set; }

        [CanBeNull]
        [Mergable]
        public IList<IAttachment> Attachments { get; set; }

        public async Task<DataTable> GetDataAsync(IRuntimeFormatter formatter)
        {
            Debug.Assert(!(formatter is null));

            if (Query.IsNullOrEmpty()) throw new InvalidOperationException("You need to specify the Query property.");

            var scope = Logger.BeginScope().AttachElapsed();
            var connectionString = ConnectionString.FormatWith(formatter);
            var query = ToString(formatter);

            try
            {
                Logger.Log(Abstraction.Layer.Database().Composite(new { properties = new { connectionString, query } }));

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = query;
                        cmd.CommandType = CommandType.Text;

                        using (var dataReader = await cmd.ExecuteReaderAsync())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(dataReader);

                            EvaluateAttachements(dataTable);

                            Logger.Log(Abstraction.Layer.Database().Meta(new { DataTable = new { RowCount = dataTable.Rows.Count, ColumnCount = dataTable.Columns.Count } }));
                            Logger.Log(Abstraction.Layer.Database().Routine(nameof(GetDataAsync)).Completed());

                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw DynamicException.Create("DataSource", $"Unable to get data for {Id}.", ex);
            }
            finally
            {
                scope.Dispose();
            }
        }

        private void EvaluateAttachements(DataTable dataTable)
        {
            foreach (var attachment in Attachments ?? Enumerable.Empty<IAttachment>())
            {
                if (dataTable.Columns.Contains(attachment.Name))
                {
                    throw DynamicException.Create("ColumnAlreadyExists", $"The data-table already contains a column with the name '{attachment.Name}'.");
                }

                dataTable.Columns.Add(new DataColumn(attachment.Name, typeof(string)));

                foreach (var dataRow in dataTable.AsEnumerable())
                {
                    try
                    {
                        var value = attachment.Compute(dataRow);
                        dataRow[attachment.Name] = value;
                    }
                    catch (Exception inner)
                    {
                        throw DynamicException.Create($"{attachment.Name}Compute", $"Could not compute the {attachment.Name} attachement.", inner);
                    }
                }
            }
        }

        public string ToString(IRuntimeFormatter formatter)
        {
            var query = Query.FormatWith(formatter);

            try
            {
                if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
                {
                    var isAbsolutePath =
                        uri.AbsolutePath.StartsWith("/") == false &&
                        Path.IsPathRooted(uri.AbsolutePath);

                    query =
                        isAbsolutePath
                            ? File.ReadAllText(uri.AbsolutePath)
                            : File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), _program.TestsDirectoryName, uri.AbsolutePath.TrimStart('/')));

                    return query.FormatWith(formatter);
                }
                else
                {
                    return query;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(ToString)).Faulted(), ex);
                return null;
            }
        }

        public IMergable New()
        {
            var mergable = _factory();
            mergable.Id = Id;
            mergable.Merge = Merge;
            return mergable;
        }
    }    
}