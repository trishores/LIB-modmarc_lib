/*
Copyright (C) 2020-2021 Tris Shores

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace marc_common
{
    internal static class c_MarcTools
    {
        internal static c_MarcVariableDataField m_GetVariableDataFieldFromAuthRecord(string v_authRecord, int[] v_targetFieldIds)
        {
            if (v_authRecord.m_IsEmpty()) return null;

            foreach (var v_targetFieldId in v_targetFieldIds)
            {
                var v_field = m_GetVariableDataFieldFromAuthRecord(v_authRecord, v_targetFieldId);
                if (v_field != null) return v_field;
            }

            return null;
        }

        // Fields 010-999
        internal static c_MarcVariableDataField m_GetVariableDataFieldFromAuthRecord(string v_authRecord, int v_targetFieldId)
        {
            return m_GetVariableDataFieldsFromAuthRecord(v_authRecord, v_targetFieldId)?.First();
        }

        // Fields 010-999
        internal static c_MarcVariableDataField[] m_GetVariableDataFieldsFromAuthRecord(string v_authRecord, int v_targetFieldId)
        {
            var v_fieldList = new List<c_MarcVariableDataField>();

            if (v_authRecord.m_IsEmpty()) return null;
            try
            {
                var v_lines = v_authRecord.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(v_x => v_x.Trim(new[] { ' ', '\t', '=' })).ToArray();
                for (var v_i = 0; v_i < v_lines.Length; v_i++)
                {
                    if (!int.TryParse(v_lines[v_i].Substring(0, 3), out int v_lineFieldId)) continue;
                    if (v_targetFieldId != v_lineFieldId) continue;

                    // remove part of line that follows an 'e.g.':
                    var v_line = v_lines[v_i].Replace("e.g. |a", "e.g. /a");
                    //var v_line = v_lines[v_i].Contains("e.g.") ? v_lines[v_i].Substring(0, v_lines[v_i].IndexOf("e.g.")) : v_lines[v_i];

                    var v_field = new c_MarcVariableDataField(v_targetFieldId);
                    //var v_test1 = Regex.Match(line, "ǂ[a-z0-9]");
                    //var v_test2 = Regex.Match(line, @"\|[a-z0-9]");
                    //var v_test3 = Regex.Match(line, @"\$[a-z0-9]");
                    var v_subfieldSymbol = Regex.IsMatch(v_line, "ǂ[a-z0-9]") ? "ǂ" : Regex.IsMatch(v_line, @"\|[a-z0-9]") ? "|" : Regex.IsMatch(v_line, @"\$[a-z0-9]") ? "$" : null;
                    if (v_subfieldSymbol == null)
                        throw new Exception("Subfield symbol not detected");
                    var v_parts = Enumerable.ToArray(v_line.Substring(3).Split(new[] { v_subfieldSymbol }, StringSplitOptions.None));
                    var v_indicators = v_parts[0].Replace("\t", "");
                    for (var v_j = 0; v_j < 3; v_j++)
                    {
                        if (v_indicators.Length == 2) break;
                        if (v_indicators.StartsWith(" ")) v_indicators = v_indicators.Substring(1);
                        if (v_indicators.EndsWith(" ")) v_indicators = v_indicators.Substring(0, v_indicators.Length - 1);
                    }
                    if (v_indicators.Length != 2)
                        throw new Exception("Indicators not detected");
                    v_field.v_Indicator1 = v_indicators[0].ToString();
                    v_field.v_Indicator2 = v_indicators[1].ToString();
                    foreach (var v_part in v_parts.Skip(1))
                    {
                        var v_subfieldId = v_part.Trim().Substring(0, 1);
                        var v_subfield = new c_MarcSubfield(v_subfieldId);
                        v_subfield.v_Data = v_part.Trim().Substring(1).Trim();
                        v_field.v_SubfieldList.Add(v_subfield);
                    }
                    v_fieldList.Add(v_field);
                }
                return v_fieldList.Count > 0 ? v_fieldList.ToArray() : null;
            }
            catch
            {
                return null;
            }
        }

        // Fields 005, 006, 007, 008
        internal static c_MarcVariableControlField m_GetVariableControlFieldFromAuthRecord(string v_authRecord, string v_targetFieldId)
        {
            if (v_authRecord.m_IsEmpty()) return null;

            var v_lines = v_authRecord.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(v_x => v_x.Trim(new[] { ' ', '\t', '=' }));
            foreach (var v_line in v_lines)
            {
                if (v_line.Length < 3 || v_targetFieldId != v_line.Substring(0, 3)) continue;

                var v_field = new c_MarcVariableControlField(v_targetFieldId);
                v_field.v_CharList = v_line.Substring(3).Trim(new[] { ' ', '\t' }).ToCharArray().ToList();

                return v_field;
            }

            return null;
        }

        internal static c_MarcVariableDataField m_GetVariableDataFieldFromFast(string v_marcBreaker)
        {
            if (v_marcBreaker.m_IsEmpty()) return null;
            try
            {
                var v_fast = v_marcBreaker.Trim(new[] { '=', ' ' });
                if (!int.TryParse(v_fast.Substring(0, 3), out int v_fieldNum))
                {
                    return null;
                }

                var v_field = new c_MarcVariableDataField(v_fieldNum);

                // parse auth field:
                var v_parts = v_fast.Substring(3).Split(new[] { "$" }, StringSplitOptions.None).ToArray();

                // indicators:
                var v_indicators = v_parts[0].Substring(v_parts[0].Length - 2);
                v_field.v_Indicator1 = v_indicators[0] == ' ' ? " " : v_indicators[0].ToString();
                v_field.v_Indicator2 = v_indicators[1] == ' ' ? " " : v_indicators[1].ToString();

                // separate subfields (don't need to know the id):
                foreach (var v_subfieldStr in v_parts.Skip(1))
                {
                    var v_subfield = new c_MarcSubfield(v_subfieldStr[0].ToString());   // subfield id e.g. a, z, etc
                    v_subfield.v_Data = v_subfieldStr.Substring(1);
                    v_field.v_SubfieldList.Add(v_subfield);
                }

                return v_field;
            }
            catch
            {
                return null;
            }
        }
    }
}
