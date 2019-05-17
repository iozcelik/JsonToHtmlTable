using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JsonToHtmlTable {
    class Program {
        static void Main(string[] args) {
            var jsonSettings = new JsonSerializerSettings() {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Include,
                Converters = { new ExpandoObjectConverter() }
            };
            var expando2 = JsonConvert.DeserializeObject<ExpandoObject>(JsonString.Readout,jsonSettings);

            var sb = new StringBuilder();
            NewMethod(sb,expando2);
            var x = sb.ToString();
        }

        private static void NewMethod(StringBuilder sb,ExpandoObject expando2) {
            var listExpandos = (expando2 as IDictionary<string,object>).Where(w => w.Value is IList).ToList();
            var otherExpandos = (expando2 as IDictionary<string,object>).Where(w => !(w.Value is IList)).ToList();


            sb.Append(@"<table>");
            var expandos = new List<ExpandoObject>();
            foreach(var item in otherExpandos) {
                if(item.Value is ExpandoObject expando) {
                    var x = expando as IDictionary<string,object>;
                    x.Add("Name",item.Key);
                    expandos.Add((ExpandoObject)x);
                } else if(item.Value is IList newList) {
                    sb.Append("<h3>").Append(item.Key).Append("</h3>");
                    foreach(var x in newList) {
                        expandos.Add((ExpandoObject)x);
                    }
                    var expandoGroups1 = GetExpandoGroups(expandos);
                    CreateExpandoGroupTable(sb,expandoGroups1);
                    expandos = new List<ExpandoObject>();
                } else if(item.Key == "Name") {
                    sb.Append("<h3>").Append(item.Value).Append("</h3>");
                } else {
                    sb.Append("<tr><td><b>").Append(item.Key).Append("</b></td><td>").Append(item.Value).Append("</td></tr>");
                }
            }
            sb.Append("</table><br><br>");

            var expandoGroups = GetExpandoGroups(expandos);
            CreateExpandoGroupTable(sb,expandoGroups);

            foreach(var listExpando in listExpandos) {
                sb.Append("<h3>").Append(listExpando.Key).Append("</h3>");
                var localExpandos = new List<ExpandoObject>();

                foreach(var x in listExpando.Value as IList) {
                    localExpandos.Add((ExpandoObject)x);
                }
                var expandoGroups1 = GetExpandoGroups(localExpandos);
                CreateExpandoGroupTable(sb,expandoGroups1);
            }
        }

        private static void CreateExpandoGroupTable(StringBuilder sb,List<(string GroupName, List<string> ColumnNames, List<ExpandoObject> Items)> expandoGroups) {
            foreach(var expandoGroup in expandoGroups) {
                sb.Append(@"<table>");
                var y = expandoGroup.Items.Select(s => s as IDictionary<string,object>);
                if(y != null && y.SelectMany(s => s.Values).Any(a => a is ExpandoObject)) {
                    var expandos = new List<ExpandoObject>();
                    foreach(var expando2 in expandoGroup.Items) {
                        NewMethod(sb,expando2);
                    }
                    var t = y.FirstOrDefault() as IDictionary<string,object>;
                    var innerGroups = GetExpandoGroups(expandos,t["Name"].ToString());
                    CreateExpandoGroupTable(sb,innerGroups);
                    continue;
                }

                sb.Append("<tr>");

                var nameColumn = expandoGroup.ColumnNames.Where(w => w == "Name");
                var otherColumns = expandoGroup.ColumnNames.Where(w => w != "Name").OrderBy(o => o);
                var columns = nameColumn.Concat(otherColumns);

                int columnPersantage = 100 / columns.Count();
                var totalUsed = columnPersantage * columns.Count();
                var nameColumnPersantage = 100 - totalUsed + columnPersantage;

                foreach(var myColumn in columns) {
                    sb.Append("<td ");
                    if(myColumn == "Name") {
                        sb.Append(@"width=""").Append(nameColumnPersantage).Append(@"%"">");
                    } else {
                        sb.Append(@"width=""").Append(columnPersantage).Append(@"%"">");
                    }
                    sb.Append(myColumn);
                    sb.Append("</td>");
                }

                sb.Append("</tr>");

                foreach(var myRow in expandoGroup.Items) {
                    sb.Append("<tr>");
                    foreach(var myColumn in columns) {
                        sb.Append("<td>");
                        var x = myRow as IDictionary<string,object>;
                        sb.Append(x[myColumn].ToString());
                        sb.Append("</td>");
                    }

                    sb.Append("</tr>");
                }
                sb.Append("</table><br><br>");

            }
        }

        private static List<(string GroupName, List<string> ColumnNames, List<ExpandoObject> Items)> GetExpandoGroups(List<ExpandoObject> expandos,string groupName = "") {
            var expandoGroups = new List<(string GroupName, List<string> ColumnNames, List<ExpandoObject> Items)>();

            for(int i = 0; i < expandos.Count; i++) {
                var propts = ((IDictionary<string,object>)expandos[i]).Select(s => s.Key).ToList();
                var expandoGroup =
                    expandoGroups.FirstOrDefault(a => a.ColumnNames.OrderBy(o => o).SequenceEqual(propts.OrderBy(o => o)));
                if(!string.IsNullOrEmpty(expandoGroup.GroupName)) {
                    expandoGroup.Items.Add(expandos[i]);
                } else {
                    expandoGroup.GroupName = string.IsNullOrEmpty(groupName) ? "Group" + (expandoGroups.Count + 1) : groupName;
                    expandoGroup.ColumnNames = new List<string>(propts);
                    expandoGroup.Items = new List<ExpandoObject>();
                    expandoGroup.Items.Add(expandos[i]);
                    expandoGroups.Add(expandoGroup);
                }
            }

            return expandoGroups;
        }
    }
}
