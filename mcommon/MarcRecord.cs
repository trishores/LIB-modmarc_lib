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
using System.Xml.Linq;
using System.Xml.Xsl;

namespace marc_common
{
    internal static class c_MarcRecord
    {
        internal static string m_GenerateMachineMarc(List<c_MarcField> v_marcOrderedFieldList)
        {
            return $"{string.Join("", v_marcOrderedFieldList.Select(v_x => v_x.v_MachineReadable))}{c_MarcSymbols.c_Machine.v_RecordTerminator}";
        }

        internal static string m_GenerateHumanMarc(List<c_MarcField> v_marcOrderedFieldList)
        {
            return string.Join("\r\n", v_marcOrderedFieldList.Select(v_x => v_x.v_HumanReadable)) + "\r\n";   // add newline to end of file to comply with convention.
        }

        internal static string m_GenerateMarcXml(List<c_MarcField> v_marcOrderedFieldList)
        {
            XNamespace v_namespace = "http://www.loc.gov/MARC21/slim";
            var v_xd = new XDocument(new XElement(v_namespace + "record", new XAttribute("xmlns", v_namespace.NamespaceName)));

            // leader:
            v_xd.Root.Add(new XElement(v_namespace + "leader", string.Join("", ((c_MarcLeaderField)v_marcOrderedFieldList.Single(v_x => v_x.v_Id == "LDR")).v_CharList)));

            // control fields:
            var v_controlTags = new[] { "001", "003", "005", "006", "007", "008" };
            void m_AddControlField(string v_tag)
            {
                var v_fields = v_marcOrderedFieldList.Where(v_x => v_x.v_Id == v_tag).Cast<c_MarcVariableControlField>().ToArray();
                if (v_fields.Length > 0)
                {
                    foreach (var v_field in v_fields)
                    {
                        v_xd.Root.Add(new XElement(v_namespace + "controlfield", new XAttribute("tag", v_tag), string.Join("", v_field.v_CharList)));
                    }
                }
            }
            v_controlTags.ToList().ForEach(v_x => m_AddControlField(v_x));

            // variable data fields:
            var v_dataTags = v_marcOrderedFieldList.Select(v_x => v_x.v_Id).Where(x => !x.m_ContainsAny(v_controlTags) && x.m_IsNumeric()).OrderBy(x => int.Parse(x)).ToArray();

            void m_AddVariableDataField(string v_tag)
            {
                var v_fields = v_marcOrderedFieldList.Where(v_x => v_x.v_Id == v_tag)?.Cast<c_MarcVariableDataField>()?.ToArray();
                if (v_fields != null && v_fields.Length > 0)
                {
                    foreach (var v_field in v_fields)
                    {
                        var v_datafield = new XElement(v_namespace + "datafield",
                            new XAttribute("tag", v_tag),
                            new XAttribute("ind1", v_field.v_Indicator1),
                            new XAttribute("ind2", v_field.v_Indicator2));
                        for (var v_i = 0; v_i < v_field.v_SubfieldList.Count; v_i++)
                        {
                            var v_subfield = v_field.v_SubfieldList[v_i];
                            var v_sfData = v_subfield.v_Data + v_subfield.v_EndPunc;
                            if (v_i == v_field.v_SubfieldList.Count - 1) v_sfData += v_field.v_EndPunc;
                            v_datafield.Add(new XElement(v_namespace + "subfield", new XAttribute("code", v_subfield.v_Id), v_sfData));
                        }
                        v_xd.Root.Add(v_datafield);
                    }
                }
            }
            v_dataTags.Distinct().ToList().ForEach(v_x => m_AddVariableDataField(v_x));

            return v_xd.ToString();
        }
    }
}