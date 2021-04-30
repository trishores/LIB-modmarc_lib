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

namespace marc_common
{
    //[Serializable]
    internal class c_MarcSubfield
    {
        internal int v_Order;
        internal string v_Id;
        private string v_data;
        internal string v_EndPunc;
        internal bool v_HideDataElementId = true;

        internal string v_Data { get => v_data; set => v_data = m_NormalizeApostrophe(value); }

        internal string v_Human
        {
            get
            {
                //if (DataElementId == "a" && HideDataElementId) return $"{Data}{EndPunc}";    // for OCLC hiding of initial $a.
                return $"{c_MarcSymbols.c_Human.v_Delimiter}{v_Id} {v_Data}{v_EndPunc}";
            }
        }

        internal string v_Machine
        {
            get
            {
                return $"{c_MarcSymbols.c_Machine.v_Delimiter}{v_Id}{v_Data}{v_EndPunc}";
            }
        }

        internal c_MarcSubfield(string v_dataElementId)
        {
            v_Id = v_dataElementId;
        }

        internal c_MarcSubfield(string v_dataElementId, string v_data)
        {
            v_Id = v_dataElementId;
            v_Data = v_data;
        }

        public override bool Equals(object v_obj)
        {
            return v_Human == ((c_MarcSubfield)v_obj).v_Human;
        }

        public override int GetHashCode()
        {
            return v_Human.GetHashCode();
        }

        public c_MarcSubfield m_DeepClone()
        {
            var v_newMarcSubfield = new c_MarcSubfield(v_Id, v_Data)
            {
                v_Order = this.v_Order,
                v_EndPunc = this.v_EndPunc,
                v_HideDataElementId = this.v_HideDataElementId,
            };

            return v_newMarcSubfield;
        }

        private string m_NormalizeApostrophe(string v_str)
        {
            if (v_str == null || v_str.Length == 0) return null;
            return v_str.Replace("�", "'");
        }
    }
}
