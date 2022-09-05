namespace SoftJail.DataProcessor
{

    using Data;
    using Newtonsoft.Json;
    using SoftJail.DataProcessor.ExportDto;
    using System;
    using System.Linq;

    public class Serializer
    {
        public static string ExportPrisonersByCells(SoftJailDbContext context, int[] ids)
        {
            var result = context.Prisoners.Where(x => ids.Contains(x.Id)).Select(x => new
            {
                Id = x.Id,
                Name = x.FullName,
                CellNumber = x.Cell.CellNumber,
                Officers = x.PrisonerOfficers.Select(po => new
                {
                    OfficerName = po.Officer.FullName,
                    Department = po.Officer.Department.Name
                }).OrderBy(x => x.OfficerName).ToArray(),
                TotalOfficerSalary = decimal.Parse(x.PrisonerOfficers.Sum(po => po.Officer.Salary).ToString("f2"))
            }).OrderBy(x => x.Name).ThenBy(x => x.Id).ToArray();

            string json = JsonConvert.SerializeObject(result, Formatting.Indented);

            return json;
        }

        public static string ExportPrisonersInbox(SoftJailDbContext context, string prisonersNames)
        {
            var names = prisonersNames.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var result = context.Prisoners.Where(x => names.Contains(x.FullName)).Select(x => new PrisonerViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                IncarcerationDate = x.IncarcerationDate.ToString("yyyy-MM-dd"),
                EncryptedMessages = x.Mails.Select(m => new EncryptedMessageViewModel
                {
                    Description = string.Join("", m.Description.Reverse())
                }).ToArray()
            }).OrderBy(x => x.FullName).ThenBy(x => x.Id).ToList();

            var xml = XmlConverter.Serialize(result, "Prisoners");

            return xml;
        }
    }
}