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
using System.Linq;
using marc_common;

namespace modmarc_lib
{
    internal class c_MarcViewerTools
    {
        internal static c_MarcVariableData m_GetVariableDataField(c_MarcDirectory.DirFieldEntry dirEntry)
        {
            var v_field = new c_MarcVariableData(dirEntry.Id);

            var v_parts = string.Join("", dirEntry.DataChars).Split(new[] { c_MarcSymbols.c_Machine.v_Delimiter, c_MarcSymbols.c_Machine.v_FieldTerminator }, StringSplitOptions.RemoveEmptyEntries);
            if (v_parts.Length < 2 || v_parts[0].Length != 2 || v_parts[1].Length < 2) return null;

            // parse indicator:
            v_field.Indicator1 = v_parts[0][0];
            v_field.Indicator2 = v_parts[0][1];

            // parse subfields:
            foreach (var v_part in v_parts.Skip(1))
            {
                var subfield = new c_MarcSubfield(v_part[0].ToString(), v_part.Substring(1));
                v_field.SubfieldList.Add(subfield);
            }

            return v_field;
        }

        internal static c_MarcDirectory.DirFieldEntry m_RepackVariableDataField(c_MarcVariableData v_field)
        {
            var v_dirEntry = new c_MarcDirectory.DirFieldEntry();

            var v_parts = string.Join("", v_dirEntry.DataChars).Split(new[] { c_MarcSymbols.c_Machine.v_Delimiter, c_MarcSymbols.c_Machine.v_FieldTerminator }, StringSplitOptions.RemoveEmptyEntries);
            if (v_parts.Length < 2 || v_parts[0].Length != 2 || v_parts[1].Length < 2)
                return null;

            // parse indicator:
            v_field.Indicator1 = v_parts[0][0];
            v_field.Indicator2 = v_parts[0][1];

            // parse subfields:
            foreach (var v_part in v_parts.Skip(1))
            {
                var v_subfield = new c_MarcSubfield(v_part[0].ToString(), v_part.Substring(1));
                v_field.SubfieldList.Add(v_subfield);
            }

            return v_dirEntry;
        }
    }
}