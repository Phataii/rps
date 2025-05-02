using rps.Models;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using rps.Data;
using System.Globalization;
using Newtonsoft.Json;

namespace rps.Services
{
    public class GradeService
    {
        private readonly ApplicationDbContext _context;
          private readonly IHttpClientFactory _httpClientFactory;
        public GradeService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> UploadGradesFromCsvAsync(IFormFile file, string type, int departmentId, string approvedby)
        {
            if (file == null || file.Length == 0)
            {
                return "No file selected";
            }

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<GradeCsvRecord>().ToList();

                    var grades = records.Select(record => new Grade
                    {
                        DepartmentId = departmentId,
                        Type = type, // Type of grade; PG or MBBS
                        GradeName = record?.GradeName?.ToUpper(),
                        GradePoint = record.GradePoint,
                        MinScore = record.MinScore,
                        MaxScore = record.MaxScore,
                        Approved = true,
                        ApprovedBy = approvedby,
                        CreatedAt = DateTime.Now,
                    }).ToList();

                    await _context.Grades.AddRangeAsync(grades);
                    await _context.SaveChangesAsync();

                    return $"{grades.Count} records uploaded successfully.";
                }
            }
            catch (Exception ex)
            {
                return $"Error uploading CSV: {ex.Message}";
            }
        }

        
        public bool UpdateGrade(Grade model)
        {
        var existing = _context.Grades.FirstOrDefault(g => g.Id == model.Id);
        if (existing == null)
            return false;

        existing.MinScore = model.MinScore;
        existing.MaxScore = model.MaxScore;
        existing.GradeName = model.GradeName;

        _context.SaveChanges();
        return true;
        }
        public async Task<List<Grade>> GradeStatus(int departmentId, bool status, string approvedBy)
        {
            var grades = await _context.Grades.Where(x => x.DepartmentId == departmentId).ToListAsync();
            
            if (!grades.Any()) return null; // Check for empty list

             grades.ForEach(g => 
            {
                g.Approved = status;
                g.ApprovedBy = approvedBy;
            });
            _context.Grades.UpdateRange(grades);
            await _context.SaveChangesAsync();
            
            return grades;
        }


        // public async Task<ApprovalHeirerchy> ApprovalHeirerchy(ApprovalHeirerchy aH, int semester, int session, int dpt, string desc)
        // {
        //     aH.Semester = semester;
        //     aH.Session = session;
        //     aH.DepartmentId = dpt;
        //     aH.Desc = desc;
        //     aH.CreatedAt = DateTime.Now;
        //     _context.ApprovalHeirerchies.Add(aH);
        //     await _context.SaveChangesAsync();
        //     return aH;
        // }
        // Create a grade
        public async Task<Grade> CreateGradeAsync(Grade grade)
        {
            grade.CreatedAt = DateTime.UtcNow;
            grade.UpdatedAt = DateTime.UtcNow;
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return grade;
        }

        // Retrieve all grades
        public async Task<List<Grade>> GetAllGradesAsync()
        {
            return await _context.Grades.ToListAsync();
        }

        // Retrieve a grade by ID
        public async Task<Grade?> GetGradeByIdAsync(int id)
        {
            return await _context.Grades.FindAsync(id);
        }

        // Update a grade
        public async Task<Grade?> UpdateGradeAsync(int id, Grade updatedGrade)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return null;

            grade.DepartmentId = updatedGrade.DepartmentId;
            grade.GradeName = updatedGrade.GradeName;
            grade.GradePoint = updatedGrade.GradePoint;
            grade.MinScore = updatedGrade.MinScore;
            grade.MaxScore = updatedGrade.MaxScore;
            grade.UpdatedAt = DateTime.UtcNow;

            _context.Grades.Update(grade);
            await _context.SaveChangesAsync();
            return grade;
        }

        // Delete a grade
        public async Task<bool> DeleteGradeAsync(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return false;

            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();
            return true;
        }


        // Setting up remark
         public async Task<string> UploadRemarkFromCsvAsync(IFormFile file, int dpt)
        {
            if (file == null || file.Length == 0)
            {
                return "No file selected";
            }

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<RemarkCsvRecord>().ToList();

                    var remark = records.Select(record => new Remark
                    {
                        //DepartmentId = departmentId,
                        From = record.From, // Type of grade; PG or MBBS
                        To = record.To,
                        RemarkSlug = record?.RemarkSlug,
                        Approved = false,
                        DepartmentId = dpt,
                        Type = "ug",
                        CreatedAt = DateTime.Now,
                    }).ToList();

                    await _context.Remarks.AddRangeAsync(remark);
                    await _context.SaveChangesAsync();

                    return $"{remark.Count} remarks uploaded successfully.";
                }
            }
            catch (Exception ex)
            {
                return $"Error uploading CSV: {ex.Message}";
            }
        }

        // ADMIN SET DEFAULT GRADE FOR ALL DEPARTMENTS
        public async Task<string> UploadGradesForDptsFromCsvAsync(IFormFile file, string type, string approvedby)
        {
            if (file == null || file.Length == 0)
            {
                return "No file selected";
            }

            // FETCH DEPARTMENTS FROM API
            string apiUrl = $"https://edouniversity.edu.ng/api/v1/departmentsapi";
            string apiKey = Environment.GetEnvironmentVariable("EUI_API_KEY");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var departments = JsonConvert.DeserializeObject<List<Departments>>(content);

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<GradeCsvRecord>().ToList();

                    var allGrades = new List<Grade>();

                    foreach (var dept in departments)
                    {
                        var deptGrades = records.Select(record => new Grade
                        {
                            DepartmentId = dept.Id,
                            Type = type,
                            GradeName = record?.GradeName?.ToUpper(),
                            GradePoint = record.GradePoint,
                            MinScore = record.MinScore,
                            MaxScore = record.MaxScore,
                            Approved = true,
                            ApprovedBy = approvedby,
                            CreatedAt = DateTime.Now,
                        }).ToList();

                        allGrades.AddRange(deptGrades);
                    }

                    await _context.Grades.AddRangeAsync(allGrades);
                    await _context.SaveChangesAsync();

                    return $"{allGrades.Count} records uploaded successfully for {departments.Count} departments.";
                }
            }
            catch (Exception ex)
            {
                return $"Error uploading CSV: {ex.Message}";
            }
        }


    }

    public class GradeCsvRecord
    {
        public string? GradeName { get; set; }
        public int GradePoint { get; set; }
        public double? MinScore { get; set; }
        public double? MaxScore { get; set; }
    }

    public class RemarkCsvRecord{
        public double From { get; set; }
        public double To { get; set; }
        public string? RemarkSlug { get; set; }

    }
}
