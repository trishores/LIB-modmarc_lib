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

using marc_common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static marc_common.c_StatusTools;

//[assembly: AssemblyKeyFile(@"..\..\signing\keyFile.snk")]

namespace modmarc_lib
{
    public class c_Engine
    {
        #region init

        public c_Engine()
        {
        }

        #endregion

        #region dat to mrc

        public int RunDatToMrc_Async(string v_data, string mrcFilePath)
        {
            try
            {
                #region write mrc

                m_ParseData(v_data, mrcFilePath);
				
                return (int)c_StatusCode.Ok;

                #endregion
            }
            catch (Exception e)
            {
                return (int)c_StatusCode.UnknownError;
            }
        }

        #endregion

        #region mrc parsing

        private void m_ParseData(string v_data, string v_mrcFilePath)
        {
            var v_lines = v_data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var v_rowsLeader = new List<string[]>();
            var v_rowsDirectory = new List<string[]>();
            var v_rowsControl = new List<string[]>();
            var v_rowsVarData = new List<string[]>();
            foreach (var v_line in v_lines)
            {
                if (v_line.StartsWith("LDR")) v_rowsLeader.Add(v_line.Substring(4).Split('\u23F5'));
                if (v_line.StartsWith("DIR")) v_rowsDirectory.Add(v_line.Substring(4).Split('\u23F5'));
                if (v_line.StartsWith("CTR")) v_rowsControl.Add(v_line.Substring(4).Split('\u23F5'));
                if (v_line.StartsWith("VAR")) v_rowsVarData.Add(v_line.Substring(4).Split('\u23F5'));
            }

            var v_fieldList = new List<c_MarcField>();

            ParseControlFields(v_rowsControl.ToList(), ref v_fieldList);
            ParseVariableDataFields(v_rowsVarData, ref v_fieldList);
            ParseDirectory(v_rowsDirectory, ref v_fieldList);
            ParseLeader(v_rowsLeader, ref v_fieldList);

            // generate filepaths:
            var filepathMrc = v_mrcFilePath;
            var filepathTxt = Path.ChangeExtension(v_mrcFilePath, ".txt");
            var filepathXml = Path.ChangeExtension(v_mrcFilePath, ".xml");

            // save machine marc record:
            var machineMarc = c_MarcRecord.m_GenerateMachineMarc(v_fieldList);
            File.WriteAllText(filepathMrc, machineMarc, c_MarcSymbols.c_Machine.v_UTF8EncodingNoBom); // must save in UTF-8 format for MARC21.

            // save text marc record:
            var humanMarc = c_MarcRecord.m_GenerateHumanMarc(v_fieldList);
            File.WriteAllText(filepathTxt, humanMarc);
            //Process.Start(filepathTxt);

            // save marcxml record:
            var marcXml = c_MarcRecord.m_GenerateMarcXml(v_fieldList);
            File.WriteAllText(filepathXml, marcXml);
        }

        private void ParseLeader(List<string[]> ldrEntries, ref List<c_MarcField> v_fieldList)
        {
            // v_fieldList must contain the directory, all control fields, & all variable data fields.
            var v_leaderField = new c_MarcLeaderField();
            var leaderLength = 24;
            var directoryLength = Encoding.UTF8.GetByteCount(v_fieldList[0].v_MachineReadable);
            v_leaderField.v_RecordLength = c_MarcLeaderField.v_Length + v_fieldList.Sum(x => Encoding.UTF8.GetByteCount(x.v_MachineReadable)) + c_MarcSymbols.c_Machine.v_RecordTerminator.ToString().Length;
            v_leaderField.v_RecordStatus = ldrEntries[1][3][0];
            v_leaderField.v_TypeOfRecord = ldrEntries[2][3][0];
            v_leaderField.v_BibliographicLevel = ldrEntries[3][3][0];
            v_leaderField.v_TypeOfControl = ldrEntries[4][3][0];
            v_leaderField.v_CharacterEncodingScheme = ldrEntries[5][3][0];
            v_leaderField.v_IndicatorCount = ldrEntries[6][3][0];
            v_leaderField.v_SubfieldCodeCount = ldrEntries[7][3][0];
            v_leaderField.v_BaseAddressOfData = leaderLength + directoryLength;
            v_leaderField.v_EncodingLevel = ldrEntries[9][3][0];
            v_leaderField.v_DescriptiveCatalogingForm = ldrEntries[10][3][0];
            v_leaderField.v_MultipartResourceRecordLevel = ldrEntries[11][3][0];
            v_leaderField.v_LengthOfLengthOfFieldPortion = ldrEntries[12][3][0];
            v_leaderField.v_LengthOfStartingCharacterPositionPortion = ldrEntries[13][3][0];
            v_leaderField.v_LengthOfImplementationDefinedPortion = ldrEntries[14][3][0];
            v_leaderField.v_Undefined = ldrEntries[15][3][0];
            v_leaderField.m_BuildCharList();

            v_fieldList.Insert(0, v_leaderField);
        }

        private void ParseDirectory(List<string[]> dirEntries, ref List<c_MarcField> v_fieldList)
        {
            // v_fieldList must contain all control fields, & all variable data fields.
            var v_dirField = new c_MarcDirectoryField();
            var v_aggregateFieldLength = 0;
            foreach (var v_field in v_fieldList)
            {
                // add field tag (3):
                v_dirField.v_CharList.AddRange(v_field.v_Id.ToCharArray());

                // add field length (4):
                var v_fieldLength = Encoding.UTF8.GetByteCount(v_field.v_MachineReadable);
                v_dirField.v_CharList.AddRange(v_fieldLength.ToString("D4").ToCharArray());

                // add field start position (5):
                v_dirField.v_CharList.AddRange(v_aggregateFieldLength.ToString("D5").ToCharArray());

                v_aggregateFieldLength += v_fieldLength;
            }
            v_fieldList.Insert(0, v_dirField); // do not reorder fieldlist after creating directory field.
        }

        private void ParseControlFields(List<string[]> v_ctrlEntries, ref List<c_MarcField> v_fieldList)
        {
            var v_ctrlFieldList = new List<c_MarcField>();
            var v_prevFieldTag = v_ctrlEntries[0][0].Substring(0, 3);

            var v_ctrlFieldEntries = new List<string[]>();
            foreach (var entry in v_ctrlEntries)
            {
                var v_fieldTag = entry[0].Substring(0, 3);
                if (v_fieldTag == v_prevFieldTag)
                {
                    v_ctrlFieldEntries.Add(entry);
                }
                else
                {
                    AddCtrlField(v_ctrlFieldEntries);
                    v_ctrlFieldEntries.Clear();
                    v_ctrlFieldEntries.Add(entry);
                    v_prevFieldTag = v_fieldTag;
                }
            }
            AddCtrlField(v_ctrlFieldEntries);

            void AddCtrlField(List<string[]> lines)
            {
                if (lines[0][0].StartsWith("001"))
                {
                    var field001 = new c_MarcVariableControlField("001");
                    foreach (var entry in lines)
                    {
                        field001.v_CharList.AddRange(entry[3].ToCharArray());
                    }
                    v_ctrlFieldList.Add(field001);
                }
                else if (lines[0][0].StartsWith("003"))
                {
                    var field003 = new c_MarcVariableControlField("003");
                    foreach (var entry in lines)
                    {
                        field003.v_CharList.AddRange(entry[3].ToCharArray());
                    }
                    v_ctrlFieldList.Add(field003);
                }
                else if (lines[0][0].StartsWith("005"))
                {
                    var field005 = new c_MarcVariableControlField("005");
                    foreach (var entry in lines)
                    {
                        field005.v_CharList.AddRange(entry[3].ToCharArray());
                    }
                    v_ctrlFieldList.Add(field005);
                }
                else if (lines[0][0].StartsWith("006"))
                {
                    var field006 = new c_MarcVariableControlField("006");
                    foreach (var entry in lines)
                    {
                        field006.v_CharList.AddRange(entry[3].ToCharArray());
                    }
                    v_ctrlFieldList.Add(field006);
                }
                else if (lines[0][0].StartsWith("007"))
                {
                    var field007 = new c_MarcVariableControlField("007");
                    foreach (var entry in lines)
                    {
                        field007.v_CharList.AddRange(entry[3].ToCharArray());
                    }
                    v_ctrlFieldList.Add(field007);
                }
                else if (lines[0][0].StartsWith("008"))
                {
                    var field008 = new c_MarcVariableControlField("008");
                    foreach (var entry in lines)
                    {
                        field008.v_CharList.AddRange(entry[3].ToCharArray());
                    }
                    v_ctrlFieldList.Add(field008);
                }
            }

            v_fieldList.AddRange(v_ctrlFieldList);
        }

        private void ParseVariableDataFields(List<string[]> varDataEntries, ref List<c_MarcField> v_fieldList)
        {
            foreach (var entry in varDataEntries)
            {
                if (entry[0].Length != 3) continue; // validation.
                if (entry[1].Length != 1) continue; // validation.
                if (entry[2].Length != 1) continue; // validation.
                if (entry[3].Length == 0) continue; // validation.

                var field = new c_MarcVariableDataField(entry[0].Substring(0, 3));
                field.v_Indicator1 = entry[1];
                field.v_Indicator2 = entry[2];
                var parts = entry[3].Split(c_MarcSymbols.c_Human.v_Delimiter, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || parts[0].Length < 3) continue; // validation.
                foreach (var part in parts)
                {
                    var subfieldId = part.Substring(0, 1);
                    var subfieldData = part == parts.Last() ? part.Substring(2) : part.Substring(2, part.Length - 3);
                    var subfield = new c_MarcSubfield(subfieldId, subfieldData);
                    field.v_SubfieldList.Add(subfield);
                }
                v_fieldList.Add(field);
            }
        }

        #endregion
    }
}