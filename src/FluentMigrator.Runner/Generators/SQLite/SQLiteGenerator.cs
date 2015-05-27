#region License
// 
// Copyright (c) 2007-2009, Sean Chambers <schambers80@gmail.com>
// Copyright (c) 2010, Nathan Brown
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using FluentMigrator.Expressions;
using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Generic;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace FluentMigrator.Runner.Generators.SQLite
{
    public class SQLiteGenerator : GenericGenerator
    {
        private readonly List<CreateForeignKeyExpression> ForeignKeys = new List<CreateForeignKeyExpression>();
        private readonly List<CreateConstraintExpression> Constraints = new List<CreateConstraintExpression>();

        public SQLiteGenerator()
            : base(new SQLiteColumn(), new SQLiteQuoter(), new EmptyDescriptionGenerator())
        {
        }

        private ForeignKeyDefinition GetFK(ColumnDefinition columnDefinition)
        {
            CreateForeignKeyExpression foreignKey = ForeignKeys.FirstOrDefault(fk => fk.ForeignKey.ForeignTable == columnDefinition.TableName
                && fk.ForeignKey.ForeignColumns.Count == 1
                && fk.ForeignKey.ForeignColumns.First() == columnDefinition.Name);

            if (foreignKey == null)
                return null;

            return foreignKey.ForeignKey;
        }

        private string GetFKSql(ColumnDefinition columnDefinition)
        {
            var fk = GetFK(columnDefinition);
            string sql = string.Empty;
            if (fk != null)
            {
                sql += "CONSTRAINT " + fk.Name 
                    + " FOREIGN KEY (" + string.Join(", ", fk.ForeignColumns.ToArray()) + ")" 
                    + " REFERENCES " + fk.PrimaryTable + " (" + string.Join(", ", fk.PrimaryColumns.ToArray()) + ")";
                if (fk.OnDelete == Rule.Cascade)
                    sql += " ON DELETE CASCADE";
            }
            return sql;
        }

        private List<ConstraintDefinition> GetConstraints(CreateTableExpression table)
        {
            List<ConstraintDefinition> constraints = Constraints
                .Where(c => c.Constraint.TableName == table.TableName)
                .Select(c => c.Constraint)
                .ToList();
            return constraints;
        }

        private string GetConstraintsSQL(CreateTableExpression table)
        {
            string sql = string.Empty;
            List<ConstraintDefinition> constraints = GetConstraints(table);
            if (constraints.Count > 0)
            {
                sql += string.Join(", ", constraints.Select(c =>
                {
                    if (c.IsUniqueConstraint)
                        return "CONSTRAINT " + c.ConstraintName + " UNIQUE (" + string.Join(", ", c.Columns.ToArray()) + ")";

                    return null;
                })
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray());
            }
            return sql;
        }

        public override string RenameTable { get { return "ALTER TABLE {0} RENAME TO {1}"; } }

        public override string Generate(CreateColumnExpression expression)
        {
            string column = base.Generate(expression);
            column += GetFKSql(expression.Column);
            return column;
        }

        public override string Generate(CreateTableExpression expression)
        {
            string sql = base.Generate(expression);

            if (sql != null && sql.EndsWith(")"))
            {
                string fks = string.Join(", ", expression.Columns.Select(c => GetFKSql(c)).Where(s => !string.IsNullOrEmpty(s)).ToArray());
                if (fks.Length > 0)
                    sql = sql.Substring(0, sql.Length - 1) + ", " + fks + ")";
            }
            if (sql != null && sql.EndsWith(")"))
            {
                string constraints = GetConstraintsSQL(expression);
                if (constraints.Length > 0)
                    sql = sql.Substring(0, sql.Length - 1) + ", " + constraints + ")";
            }

            return sql;
        }

        public override string Generate(AlterColumnExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("SQLite does not support alter column");
        }

        public override string Generate(RenameColumnExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("SQLite does not support renaming of columns");
        }

        public override string Generate(DeleteColumnExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("SQLite does not support deleting of columns");
        }

        public override string Generate(AlterDefaultConstraintExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("SQLite does not support altering of default constraints");
        }

        public override string Generate(CreateForeignKeyExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("SQLite Foreign keys can only be added when the table is created");
        }

        public override string Generate(DeleteForeignKeyExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("SQLite Foreign keys can only be deleted when the entire table is deleted");
        }

        public override string Generate(CreateSequenceExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("Sequences are not supported in SQLite");
        }

        public override string Generate(DeleteSequenceExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("Sequences are not supported in SQLite");
        }

        public override string Generate(DeleteDefaultConstraintExpression expression)
        {
            return compatabilityMode.HandleCompatabilty("Default constraints are not supported");
        }
    }
}
