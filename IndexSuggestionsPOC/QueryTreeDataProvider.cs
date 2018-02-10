using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class QueryTreeDataProvider
    {
        private readonly JObject jObject;
        private readonly Lazy<List<TableInfo>> fromTables;
        private readonly Lazy<List<JoinInfo>> joins;
        private readonly Lazy<List<PredicateInfo>> wherePredicates;
        private readonly Lazy<List<PredicateInfo>> havingPredicates;
        private readonly Lazy<CmdType> commandType;

        private string fromTablesHash = null;

        public List<TableInfo> FromTables
        {
            get { return fromTables.Value; }
        }
        public List<JoinInfo> Joins
        {
            get { return joins.Value; }
        }
        public List<PredicateInfo> WherePredicates
        {
            get { return wherePredicates.Value; }
        }
        public List<PredicateInfo> HavingPredicates
        {
            get { return havingPredicates.Value; }
        }
        public CmdType CommandType
        {
            get { return commandType.Value; }
        }

        public string FromTablesHash
        {
            get
            {
                if (fromTablesHash == null)
                {
                    fromTablesHash = String.Join("_", FromTables.OrderBy(x => x.ID).Select(x => x.ID.ToString()));
                }
                return fromTablesHash;
            }
        }
        public QueryTreeDataProvider(JObject jObject)
        {
            this.jObject = jObject;
            this.fromTables = new Lazy<List<TableInfo>>(InitializeFromTables);
            this.joins = new Lazy<List<JoinInfo>>(InitializeJoins);
            this.commandType = new Lazy<CmdType>(InitializeCommandType);
            this.wherePredicates = new Lazy<List<PredicateInfo>>(() => InitializePredicates(@"QUERY.jointree.FROMEXPR.quals"));
            this.havingPredicates = new Lazy<List<PredicateInfo>>(() => InitializePredicates(@"..havingQual"));
        }

        private CmdType InitializeCommandType()
        {
            var cmdType = jObject.SelectToken("QUERY.commandType");
            return EnumParsingSupport.ConvertFromNumericOrDefault<CmdType>(cmdType.Value<int>());
        }

        private List<TableInfo> InitializeFromTables()
        {
            List<TableInfo> result = new List<TableInfo>();
            var rtes = jObject.SelectTokens("..RTE");
            foreach (var rte in rtes)
            {
                var rteKind = EnumParsingSupport.ConvertFromNumericOrDefault<RteKind>(rte.SelectToken("rtekind").Value<int>());
                if (rteKind == RteKind.Relation)
                {
                    var relKind = EnumParsingSupport.ConvertFromStringOrDefault<RelKind>(rte.SelectToken("relkind").Value<string>());
                    if (relKind == RelKind.Relation)
                    {
                        TableInfo toAdd = new TableInfo();
                        toAdd.ID = rte.SelectToken("relid").Value<long>();
                        var table = PostgresData.Instance.PgClasses.Where(x => x.oid == toAdd.ID).First();
                        toAdd.Name = table.relname;
                        var schema = PostgresData.Instance.PgNamespaces.Where(x => x.oid == table.relnamespace).First();
                        toAdd.SchemaName = schema.nspname;
                        result.Add(toAdd);
                    }
                }
            }
            return result;
        }

        private List<JoinInfo> InitializeJoins()
        {
            List<JoinInfo> result = new List<JoinInfo>();
            var joins = jObject.SelectTokens("..JOINEXPR");
            foreach (var join in joins)
            {
                var toAdd = new JoinInfo();
                toAdd.Type = EnumParsingSupport.ConvertFromNumericOrDefault<JoinType>(join.SelectToken("jointype").Value<int>());
                var index = join.SelectToken("larg").SelectToken("RANGETBLREF").SelectToken("rtindex").Value<int>() - 1;
                toAdd.LeftRelation = FromTables[index];
                index = join.SelectToken("rarg").SelectToken("RANGETBLREF").SelectToken("rtindex").Value<int>() - 1;
                toAdd.RightRelation = FromTables[index];
                toAdd.Predicates.AddRange(InitializePredicates("..JOINEXPR.quals"));
                result.Add(toAdd);
            }
            return result;
        }

        private List<PredicateInfo> InitializePredicates(string qualsQuery)
        {
            List<PredicateInfo> result = new List<PredicateInfo>();
            var conditions = jObject.SelectToken(qualsQuery).SelectTokens("..OPEXPR");
            foreach (var condition in conditions)
            {
                PredicateInfo predicate = new PredicateInfo();
                var op = predicate.Operator = new PredicateOperator();
                op.ID = condition.SelectToken("opno").Value<long>();
                var postgresOperator = PostgresData.Instance.PgOperators.Where(x => x.oid == op.ID).First();
                op.Name = EnumParsingSupport.ConvertFromStringOrDefault<PredicateOperatorName>(postgresOperator.oprname);
                // todo - only bool = opresulttype == 16 (pg_type oid = 16)
                var constants = condition.SelectTokens("..CONST");
                foreach (var c in constants)
                {
                    ConstantValue cToAdd = new ConstantValue();
                    var constTypeOid = c.SelectToken("consttype").Value<long>();
                    var constValue = c.SelectToken("constvalue").Value<string>();
                    var postgresType = PostgresData.Instance.PgTypes.Where(x => x.oid == constTypeOid).First();
                    cToAdd.Type = EnumParsingSupport.ConvertFromStringOrDefault<SqlDataType>(postgresType.typname);
                    cToAdd.Value = new ConstantValueConverter().Convert(cToAdd.Type, constValue);
                    predicate.Operands.Add(cToAdd);
                }
                // todo vars
                var vars = condition.SelectTokens("..VAR");
                foreach (var v in vars)
                {
                    AttributeOperand toAdd = new AttributeOperand();
                    var rteIndex = v.SelectToken("varno").Value<int>() - 1;
                    toAdd.Table = FromTables[rteIndex];
                    var typeOid = v.SelectToken("vartype").Value<long>();
                    var postgresType = PostgresData.Instance.PgTypes.Where(x => x.oid == typeOid).First();
                    toAdd.Type = EnumParsingSupport.ConvertFromStringOrDefault<SqlDataType>(postgresType.typname);
                    var attno = v.SelectToken("varattno").Value<long>();
                    var postgresAttribute = PostgresData.Instance.PgAttributes.Where(x => x.attrelid == toAdd.Table.ID && x.attnum == attno).First();
                    toAdd.AttributeName = postgresAttribute.attname;
                    predicate.Operands.Add(toAdd);
                }
                result.Add(predicate);
            }
            return result;
        }
    }

    class ConstantValueConverter
    {
        public object Convert(SqlDataType type, string str)
        {
            switch (type)
            {
                case SqlDataType.Int16:
                    return BitConverter.ToInt16(GetIntegerByteArrayRepresentation(str), 0);
                case SqlDataType.Int32:
                    return BitConverter.ToInt32(GetIntegerByteArrayRepresentation(str), 0);
                case SqlDataType.Int64:
                    return BitConverter.ToInt64(GetIntegerByteArrayRepresentation(str), 0);
                case SqlDataType.VarChar:
                case SqlDataType.Text:
                    return Encoding.GetEncoding("windows-1250").GetString(GetStringByteArrayRepresentation(str));
            }
            return null;
        }

        private byte[] GetIntegerByteArrayRepresentation(string str)
        {
            var inputValue = str.Substring(str.IndexOf("[") + 1); // take after [
            inputValue = inputValue.Substring(0, inputValue.Length - 1); // remove ]
            var bytesStr = new List<string>(inputValue.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries));
            List<byte> bytes = new List<byte>();
            bytesStr.ForEach(x =>
            {
                int b = int.Parse(x);
                if (b < 0)
                {
                    b = b + 256;
                }
                bytes.Add((byte)b);
            });
            return bytes.ToArray();
        }

        // string format is [ some code 0 0 0 data] - goal ist to process only data
        private byte[] GetStringByteArrayRepresentation(string str)
        {
            var inputValue = str.Substring(str.IndexOf("[") + 1); // take after [
            inputValue = inputValue.Substring(0, inputValue.Length - 1); // remove ]
            var bytesStr = new List<string>(inputValue.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries));
            List<byte> bytes = new List<byte>();
            for (int i = bytesStr.Count - 1; i >= 0; i--)
            {
                string x = bytesStr[i];
                int b = int.Parse(x);
                if (b == 0)
                {
                    break;
                }
                if (b < 0)
                {
                    b = b + 256;
                }
                bytes.Insert(0, (byte)b);
            }
            return bytes.ToArray();
        }
    }

    class TableInfo
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public string SchemaName { get; set; }
    }
    class JoinInfo
    {
        public JoinType Type { get; set; }
        public TableInfo LeftRelation { get; set; }
        public TableInfo RightRelation { get; set; }
        public List<PredicateInfo> Predicates { get; private set; }

        public JoinInfo()
        {
            Predicates = new List<PredicateInfo>();
        }

    }
    // primnodes.h opexrp
    class PredicateInfo
    {
        public PredicateOperator Operator { get; set; }
        public List<PredicateOperand> Operands { get; private set; }
        public PredicateInfo()
        {
            Operands = new List<PredicateOperand>();
        }
    }

    class PredicateOperand
    {
        public SqlDataType Type { get; set; }
    }

    class AttributeOperand : PredicateOperand
    {
        public TableInfo Table { get; set; }
        public string AttributeName { get; set; }
    }

    class ConstantValue : PredicateOperand
    {
        public object Value { get; set; }
    }

    // todo - use NpgsqlDbType and (try to) map to DbType
    enum SqlDataType
    {
        Unknown = 0,
        [EnumMember(Value = "bool")]
        Bool = 1,
        [EnumMember(Value = "bytea")]
        ByteArray = 2,
        [EnumMember(Value = "char")]
        Char = 3,
        [EnumMember(Value = "int2")]
        Int16 = 4,
        [EnumMember(Value = "int4")]
        Int32 = 5,
        [EnumMember(Value = "int8")]
        Int64 = 6,
        [EnumMember(Value = "varchar")]
        VarChar = 10,
        [EnumMember(Value = "text")]
        Text = 11
    }

    class PredicateOperator
    {
        public long ID { get; set; }
        public PredicateOperatorName Name { get; set; }
    }

    enum PredicateOperatorName
    {
        Unknown = 0,
        [EnumMember(Value = "=")]
        Equal = 1,
        [EnumMember(Value = "<>")]
        NotEqual = 2,
        [EnumMember(Value = "<")]
        LowerThan = 3,
        [EnumMember(Value = "<=")]
        LowerThanOrEqual = 4,
        [EnumMember(Value = ">")]
        GreaterThan = 5,
        [EnumMember(Value = ">=")]
        GreaterThanOrEqual = 6
    }

    /*nodes.h
     *     CMD_UNKNOWN,
	CMD_SELECT,					// select stmt 
	CMD_UPDATE,					// update stmt 
	CMD_INSERT,					// insert stmt 
	CMD_DELETE,
	CMD_UTILITY,				// cmds like create, destroy, copy, vacuum,
								 * etc. 
	CMD_NOTHING					// dummy command for instead nothing rules
								 * with qual 
     */
    enum CmdType
    {
        Unknown = 0,
        Select = 1,
        Update = 2,
        Insert = 3,
        Delete = 4,
        Utility = 5,
        Nothing = 6
    }

/*parsenodes.h
    RTE_RELATION,				// ordinary relation reference 
    RTE_SUBQUERY,				// subquery in FROM 
	RTE_JOIN,					// join 
	RTE_FUNCTION,				// function in FROM 
	RTE_TABLEFUNC,				// TableFunc(.., column list) 
	RTE_VALUES,					// VALUES (<exprlist>), (<exprlist>), ... 
	RTE_CTE,					// common table expr (WITH list element) 
	RTE_NAMEDTUPLESTORE			// tuplestore, e.g. for AFTER triggers 
 */
    enum RteKind
    {
        Relation = 0,
        Subquery = 1,
        Join = 2,
        Function = 3,
        TableFunc = 4,
        Values = 5,
        CTE = 6,
        NamedTupleStore = 7
    }
 
/*pg_class.h
#define RELKIND_RELATION		  'r'	// ordinary table
#define RELKIND_INDEX			  'i'	// secondary index
#define RELKIND_SEQUENCE		  'S'	// sequence object
#define RELKIND_TOASTVALUE	  't'	// for out-of-line values
#define RELKIND_VIEW			  'v'	// view
#define RELKIND_MATVIEW		  'm'	// materialized view
#define RELKIND_COMPOSITE_TYPE  'c'	// composite type
#define RELKIND_FOREIGN_TABLE   'f'	// foreign table
#define RELKIND_PARTITIONED_TABLE 'p' // partitioned table
*/
    enum RelKind
    {
        Unknown = 0,
        [EnumMember(Value = "r")]
        Relation = 1,
        [EnumMember(Value = "i")]
        Index = 2,
        [EnumMember(Value = "S")]
        Sequence = 3,
        [EnumMember(Value = "t")]
        ToastValue = 4,
        [EnumMember(Value = "v")]
        View = 5,
        [EnumMember(Value = "m")]
        MaterializedView = 6,
        [EnumMember(Value = "c")]
        CompositeType = 7,
        [EnumMember(Value = "f")]
        ForeignTable = 8,
        [EnumMember(Value = "p")]
        PartitionedTable = 9
    }

    enum JoinType
    {
        Inner = 0,
        LeftOuter = 1,
        FullOuter = 2,
        RightOuter = 3,
        Semi = 4,
        AntiSemi = 5,
        UniqueOuter = 6,
        UniqueInner = 7
    }
}
