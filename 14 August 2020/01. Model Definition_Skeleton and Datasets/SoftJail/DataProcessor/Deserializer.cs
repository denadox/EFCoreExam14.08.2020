namespace SoftJail.DataProcessor
{

    using Data;
    using Newtonsoft.Json;
    using SoftJail.Data.Models;
    using SoftJail.Data.Models.Enums;
    using SoftJail.DataProcessor.ImportDto;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class Deserializer
    {
        public static string ImportDepartmentsCells(SoftJailDbContext context, string jsonString)
        {
            var sb = new StringBuilder();
            var departments = new List<Department>();
            var departmentCells = JsonConvert.DeserializeObject<IEnumerable<DepartmentCellInputModel>>(jsonString);

            foreach (var departmentCell in departmentCells)
            {
                if (!IsValid(departmentCell) || !departmentCell.Cells.All(IsValid) || departmentCell.Cells.Count == 0)
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }
                var department = new Department
                {
                    Name = departmentCell.Name,
                    Cells = departmentCell.Cells.Select(x => new Cell
                    {
                        CellNumber = x.CellNumber,
                        HasWindow = x.HasWindow
                    }).ToList()
                };
                departments.Add(department);
                sb.AppendLine($"Imported {department.Name} with {department.Cells.Count} cells");
            }
            context.Departments.AddRange(departments);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        public static string ImportPrisonersMails(SoftJailDbContext context, string jsonString)
        {
            var sb = new StringBuilder();
            var prisoners = new List<Prisoner>();
            var prisonerMails = JsonConvert.DeserializeObject<IEnumerable<PrisonerMailInputModel>>(jsonString);

            foreach (var prisoner in prisonerMails)
            {
                if (!IsValid(prisoner) || !prisoner.Mails.All(IsValid))
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }

                var releaseDate = DateTime.TryParseExact(prisoner.ReleaseDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime rd);
                var incarcerationDate = DateTime.ParseExact(prisoner.IncarcerationDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var currentPrisoner = new Prisoner
                {
                    FullName = prisoner.FullName,
                    Nickname = prisoner.Nickname,
                    Age = prisoner.Age,
                    IncarcerationDate = incarcerationDate,
                    ReleaseDate = rd,
                    Bail = prisoner.Bail,
                    CellId = prisoner.CellId,
                    Mails = prisoner.Mails.Select(x => new Mail
                    {
                        Sender = x.Sender,
                        Address = x.Address,
                        Description = x.Description
                    }).ToList()
                };

                prisoners.Add(currentPrisoner);
                sb.AppendLine($"Imported {currentPrisoner.FullName} {currentPrisoner.Age} years old");
            }
            context.Prisoners.AddRange(prisoners);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        public static string ImportOfficersPrisoners(SoftJailDbContext context, string xmlString)
        {
            var sb = new StringBuilder();
            var validOfficers = new List<Officer>();
            var officerPrisoners = XmlConverter.Deserializer<OfficerPrisonerInputModel>(xmlString, "Officers");

            foreach (var officerPrisoner in officerPrisoners)
            {
                if (!IsValid(officerPrisoner))
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }
                var officer = new Officer
                {
                    FullName = officerPrisoner.Name,
                    Salary = officerPrisoner.Money,
                    DepartmentId = officerPrisoner.DepartmentId,
                    Position = Enum.Parse<Position>(officerPrisoner.Position),
                    Weapon = Enum.Parse<Weapon>(officerPrisoner.Weapon),
                    OfficerPrisoners = officerPrisoner.Prisoners.Select(x => new OfficerPrisoner
                    {
                        PrisonerId = x.Id
                    }).ToList()
                };
                validOfficers.Add(officer);
                sb.AppendLine($"Imported {officer.FullName} ({officer.OfficerPrisoners.Count} prisoners)");
            }
            context.Officers.AddRange(validOfficers);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object obj)
        {
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(obj);
            var validationResult = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(obj, validationContext, validationResult, true);
            return isValid;
        }
    }
}