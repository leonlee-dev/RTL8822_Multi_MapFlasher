using System;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Model;
using IndentedTextWriter = System.Data.Entity.Migrations.Utilities.IndentedTextWriter;

namespace MapFlasher
{
    public class ExtendedMigrationCodeGenerator : CSharpMigrationCodeGenerator
    {
        protected override void Generate(ColumnModel column, IndentedTextWriter writer, bool emitName = false)
        {
            if (column.Annotations.Keys.Contains("DefaultBoolean"))
            {
                var value = Convert.ChangeType(column.Annotations["DefaultBoolean"].NewValue, column.ClrDefaultValue.GetType());
                //Console.WriteLine("+" + column.Annotations["Default"].NewValue);
                //Console.WriteLine("-" + column.ClrDefaultValue.GetType());
                //Console.WriteLine("*" + value);
                column.DefaultValue = value;
            }

            if (column.Annotations.Keys.Contains("DefaultUsedState"))
            {
                var value = Convert.ChangeType(column.Annotations["DefaultUsedState"].NewValue, column.ClrDefaultValue.GetType());
                column.DefaultValue = value;
            }

            base.Generate(column, writer, emitName);
        }
    }
}