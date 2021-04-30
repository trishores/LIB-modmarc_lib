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

namespace marc_common
{
    //[Serializable]
    internal abstract class c_MarcField
    {
        private string v_id;
        internal string v_Id
        {
            get { return v_id; }
            set
            {
                v_id = value;
                if (value == "LDR") v_Order = 0;
                else if (value == "DIR") v_Order = 1;
                else if (value.StartsWith("6") && value != "655") v_Order = 620;    // default order value.
                else if (value.m_IsNumeric()) v_Order = int.Parse(value);
                else throw new Exception("Unknown marc field order");
            }
        }
        internal double v_Order { get; set; }
        internal abstract string v_HumanReadable { get; }
        internal abstract string v_MachineReadable { get; }
    }

    // The Leader contains data elements that primarily provide information for the processing of the record. The data elements contain numbers or coded values and are identified by relative character position. The Leader is fixed in length at 24 character positions and is the first field of a MARC record. Only ascii (8-bit) characters are permitted within the Leader field. The Leader does not end with a field termination character. 
    internal class c_MarcLeaderField : c_MarcField
    {
        internal List<char> v_CharList = new List<char>();

        internal int v_RecordLength;
        internal char v_RecordStatus = 'n';
        internal char v_TypeOfRecord = 'a';
        internal char v_BibliographicLevel = 'm';
        internal char v_TypeOfControl = ' ';
        internal char v_CharacterEncodingScheme = 'a';
        internal char v_IndicatorCount = '2';
        internal char v_SubfieldCodeCount = '2';
        internal int v_BaseAddressOfData;
        internal char v_EncodingLevel = ' ';
        internal char v_DescriptiveCatalogingForm = 'i';
        internal char v_MultipartResourceRecordLevel = ' ';
        internal char v_LengthOfLengthOfFieldPortion = '4';
        internal char v_LengthOfStartingCharacterPositionPortion = '5';
        internal char v_LengthOfImplementationDefinedPortion = '0';
        internal char v_Undefined = '0';

        internal c_MarcLeaderField()
        {
            v_Id = "LDR";
        }

        internal override string v_HumanReadable =>
            $"{v_Id} {string.Join("", v_CharList)}";

        internal override string v_MachineReadable =>
            $"{string.Join("", v_CharList)}";

        internal void m_BuildCharList()
        {
            v_CharList.AddRange(v_RecordLength.ToString("D5").ToCharArray());
            v_CharList.Add(v_RecordStatus);
            v_CharList.Add(v_TypeOfRecord);
            v_CharList.Add(v_BibliographicLevel);
            v_CharList.Add(v_TypeOfControl);
            v_CharList.Add(v_CharacterEncodingScheme);
            v_CharList.Add(v_IndicatorCount);
            v_CharList.Add(v_SubfieldCodeCount);
            v_CharList.AddRange(v_BaseAddressOfData.ToString("D5").ToCharArray());
            v_CharList.Add(v_EncodingLevel);
            v_CharList.Add(v_DescriptiveCatalogingForm);
            v_CharList.Add(v_MultipartResourceRecordLevel);
            v_CharList.Add(v_LengthOfLengthOfFieldPortion);
            v_CharList.Add(v_LengthOfStartingCharacterPositionPortion);
            v_CharList.Add(v_LengthOfImplementationDefinedPortion);
            v_CharList.Add(v_Undefined);
        }

        internal const int v_Length = 24;
    }

    // The Directory is a series of entries that contain the tag, length, and starting location of each variable field within a record. Each entry is 12 character positions in length. Directory entries for variable control fields appear first, sequenced by the field tag in ascending order. Entries for variable data fields follow, sequenced by the first character of the field tag in ascending order. The stored sequence of the variable data fields in a record does not necessarily correspond to the order of the corresponding Directory entries. Duplicate tags are distinguished only by the location of the respective fields within the record. Only ascii (8-bit) characters are permitted within the Directory field. The Directory ends with a field termination character (ascii 1E).
    internal class c_MarcDirectoryField : c_MarcField
    {
        internal List<char> v_CharList = new List<char>();

        internal c_MarcDirectoryField()
        {
            v_Id = "DIR";
        }

        internal override string v_HumanReadable =>
            $"{v_Id} {string.Join("", v_CharList)}";

        internal override string v_MachineReadable =>
            $"{string.Join("", v_CharList)}{c_MarcSymbols.c_Machine.v_FieldTerminator}";
    }

    // The Variable Control Fields (001, 005, 006, 007, 008) contains no indicators or subfields. Note that the term 'Variable' is misleading since all but 007 are fixed-length fields. Every Variable Control Field ends with a field termination character.
    internal class c_MarcVariableControlField : c_MarcField
    {
        internal List<char> v_CharList = new List<char>();

        internal c_MarcVariableControlField(int v_id)
        {
            v_Id = v_id.ToString();
        }

        internal c_MarcVariableControlField(string v_id)
        {
            v_Id = v_id;
        }

        internal override string v_HumanReadable =>
            $"{v_Id} {string.Join("", v_CharList)}";

        internal override string v_MachineReadable =>
            $"{string.Join("", v_CharList)}{c_MarcSymbols.c_Machine.v_FieldTerminator}";

        public override bool Equals(object v_obj)
        {
            return v_HumanReadable == ((c_MarcVariableControlField)v_obj).v_HumanReadable;
        }

        public override int GetHashCode()
        {
            return v_HumanReadable.GetHashCode();
        }
    }

    // The Variable Data Fields (010 through 999) vary in length and can have between 1 and 9,999 characters. They have a 3-digit numeric value, 2 indicator positions (blank or 0-9 as possible values), and subfields. Subfields are a textual element identified by a delimiter symbol and a lowercase alphabetic or numeric code. Every Variable Data Field ends with a field termination character.
    //[Serializable]
    internal class c_MarcVariableDataField : c_MarcField
    {
        internal string v_Indicator1;
        internal string v_Indicator2;
        internal List<c_MarcSubfield> v_SubfieldList = new List<c_MarcSubfield>();
        internal string v_EndPunc;
        internal object v_Tag;

        private int v_currSubfieldIndex;

        internal c_MarcVariableDataField() { }

        internal c_MarcVariableDataField(int v_id)
        {
            v_Id = v_id.ToString();
        }

        internal c_MarcVariableDataField(string v_id)
        {
            v_Id = v_id;
        }

        internal override string v_HumanReadable =>
            $"{v_Id} {v_Indicator1.Replace(" ", "_")}{v_Indicator2.Replace(" ", "_")} {string.Join(" ", v_SubfieldList.Select(v_x => v_x.v_Human))}{v_EndPunc}";

        internal override string v_MachineReadable =>
            $"{v_Indicator1.Replace("_", " ")}{v_Indicator2.Replace("_", " ")}{string.Join("", v_SubfieldList.Select(v_x => v_x.v_Machine))}{v_EndPunc}{c_MarcSymbols.c_Machine.v_FieldTerminator}";

        internal void m_SetCurrSubfield(int v_i)
        {
            v_currSubfieldIndex = v_i;
        }

        internal c_MarcSubfield v_CurrSubfield
        {
            get
            {
                if (v_SubfieldList.Count == 0) return null;
                return v_SubfieldList[v_currSubfieldIndex];
            }
        }

        internal c_MarcSubfield v_PrevSubfield
        {
            get
            {
                return v_currSubfieldIndex - 1 < 0 ? null : v_SubfieldList[v_currSubfieldIndex - 1];
            }
        }

        internal c_MarcSubfield v_NextSubfield
        {
            get
            {
                return v_currSubfieldIndex + 1 > v_SubfieldList.Count - 1 ? null : v_SubfieldList[v_currSubfieldIndex + 1];
            }
        }

        public override bool Equals(object v_obj)
        {
            return v_HumanReadable == ((c_MarcVariableDataField)v_obj).v_HumanReadable;
        }

        public override int GetHashCode()
        {
            return v_HumanReadable.GetHashCode();
        }

        public c_MarcVariableDataField m_DeepClone()
        {
            var v_newMarcVariableDataField = new c_MarcVariableDataField()
            {
                v_Id = this.v_Id,
                v_Indicator1 = this.v_Indicator1,
                v_Indicator2 = this.v_Indicator2,
                v_SubfieldList = new List<c_MarcSubfield>(),
                v_EndPunc = this.v_EndPunc,
                v_Tag = this.v_Tag,
            };
            this.v_SubfieldList.ForEach(v_x => v_newMarcVariableDataField.v_SubfieldList.Add(v_x.m_DeepClone()));

            return v_newMarcVariableDataField;
        }
    }
}

// OCLC refers to the Leader and 008 as a single combined data unit called the Fixed field. Each element in the Fixed field has a mnemonic label. 