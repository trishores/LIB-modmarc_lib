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

using System.Linq;

namespace modmarc_lib
{
    internal class m_MarcLeader
    {
        internal int RecordLength;
        internal char RecordStatus = 'n';
        internal char TypeOfRecord = 'a';
        internal char BibliographicLevel = 'm';
        internal char TypeOfControl = ' ';
        internal char CharacterEncodingScheme = 'a';
        internal char IndicatorCount = '2';
        internal char SubfieldCodeCount = '2';
        internal int BaseAddressOfData;
        internal char EncodingLevel = ' ';
        internal char DescriptiveCatalogingForm = 'i';
        internal char MultipartResourceRecordLevel = ' ';
        internal char LengthOfLengthOfFieldPortion = '4';
        internal char LengthOfStartingCharacterPositionPortion = '5';
        internal char LengthOfImplementationDefinedPortion = '0';
        internal char Undefined = '0';

        internal m_MarcLeader(string mrcContent)
        {
            var charList = mrcContent.ToCharArray().ToList().GetRange(0, 23);

            RecordLength = int.Parse(string.Join("", charList.GetRange(0, 5)));
            BaseAddressOfData = int.Parse(string.Join("", charList.GetRange(12, 5)));
            EncodingLevel = charList[17];
        }
    }
}
