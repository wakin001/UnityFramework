using MasterMemory;
using MessagePack;
using System.Collections.Generic;
using System;
using MasterData.Tables;

namespace MasterData
{
   public sealed class MemoryDatabase : MemoryDatabaseBase
   {
        public PersonTable PersonTable { get; private set; }

        public MemoryDatabase(
            PersonTable PersonTable
        )
        {
            this.PersonTable = PersonTable;
        }

        public MemoryDatabase(byte[] databaseBinary, bool internString = true, MessagePack.IFormatterResolver formatterResolver = null)
            : base(databaseBinary, internString, formatterResolver)
        {
        }

        protected override void Init(Dictionary<string, (int offset, int count)> header, int headerOffset, byte[] databaseBinary, MessagePack.IFormatterResolver resolver)
        {
            this.PersonTable = ExtractTableData<Person, PersonTable>(header, headerOffset, databaseBinary, resolver, xs => new PersonTable(xs));
        }

        public ImmutableBuilder ToImmutableBuilder()
        {
            return new ImmutableBuilder(this);
        }

        public DatabaseBuilder ToDatabaseBuilder()
        {
            var builder = new DatabaseBuilder();
            builder.Append(this.PersonTable.GetRawDataUnsafe());
            return builder;
        }
    }
}