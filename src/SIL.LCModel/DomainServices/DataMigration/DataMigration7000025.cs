// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
// 
// File: DataMigration7000025.cs
// Responsibility: Randy Regnier

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.LCModel.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000024 to 7000025.
	/// 
	/// This DM switches persisted DateTime data to be UTC,
	/// and assumes the data to be changed is local time on the comuter doing the DM.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000025 : IDataMigration
	{
		#region Implementation of IDataMigration

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change all guids to lowercase to help the Chorus diff/merge code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000024);

			const string dateCreated = "DateCreated";
			var properties = new List<string> { dateCreated, "DateModified" };

			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("CmProject"), properties); // Tested
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("CmMajorObject"), properties); // Tested
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("CmPossibility"), properties); // Tested
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("CmAnnotation"), properties); // Tested
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("StJournalText"), properties); // Tested
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("LexEntry"), properties); // Tested
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("RnGenericRec"), properties); // Tested
			// Since ScrScriptureNote derives from CmBaseAnnotation, which has already had it DateCreated & DateModified updated,
			// we have to clear those two out of the dictioanry, or they will be updated twice, and the test on ScrScriptureNote will fail.
			properties.Clear();
			properties.Add("DateResolved");
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("ScrScriptureNote"), properties); // Tested
			properties.Clear();
			properties.Add(dateCreated);
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("ScrDraft"), properties);
			properties.Clear();
			properties.Add("RunDate");
			ConvertClassAndSubclasses(domainObjectDtoRepository, domainObjectDtoRepository.AllInstancesWithSubclasses("ScrCheckRun"), properties);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		private static void ConvertClassAndSubclasses(IDomainObjectDTORepository domainObjectDtoRepository,
			IEnumerable<DomainObjectDTO> allInstancesWithSubclasses, IEnumerable<string> dateTimeProperties)
		{
			foreach (var dto in allInstancesWithSubclasses)
			{
				var rtElement = XElement.Parse(dto.Xml);
				var wasChanged = false;
				foreach (var propElement in dateTimeProperties
					.Select(propName => rtElement.Element(propName)).Where(propElement => propElement != null))
				{
					ConvertLocalToUtc(propElement.Attribute("val"));
					wasChanged = true;
				}
				if (wasChanged)
					DataMigrationServices.UpdateDTO(domainObjectDtoRepository, dto, rtElement.ToString());
			}
		}

		private static void ConvertLocalToUtc(XAttribute localAttr)
		{
			var dtParts = localAttr.Value.Split(new[] { '-', ' ', ':', '.' });
			var asLocal = new DateTime(
				Int32.Parse(dtParts[0]),
				Int32.Parse(dtParts[1]),
				Int32.Parse(dtParts[2]),
				Int32.Parse(dtParts[3]),
				Int32.Parse(dtParts[4]),
				Int32.Parse(dtParts[5]),
				Int32.Parse(dtParts[6]));
			var asUtc = asLocal.ToUniversalTime();
			localAttr.Value = String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
			                                asUtc.Year,
			                                asUtc.Month,
			                                asUtc.Day,
			                                asUtc.Hour,
			                                asUtc.Minute,
			                                asUtc.Second,
			                                asUtc.Millisecond);
		}

		#endregion
	}
}
