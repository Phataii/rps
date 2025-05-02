using rps.Models;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using rps.Data;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;


namespace rps.Services
{
    public class ResultService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        public ResultService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
             _httpClientFactory = httpClientFactory;
        }


        public class UploadResultResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public int Count { get; set; }
        }

        public async Task<UploadResultResponse> UploadResultFromCsvAsync(IFormFile file, string courseId, int sessionId, int semesterId, int levelId, string uploader, string userId)
        {
           try
            {
                // FETCH DEPARTMENTS FROM API
                string apiUrl = $"https://edouniversity.edu.ng/api/v1/departmentsapi";
                string apiKey = Environment.GetEnvironmentVariable("EUI_API_KEY");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var departments = JsonConvert.DeserializeObject<List<Departments>>(content);

                // Create Department Name -> Id dictionary for faster lookup
                var departmentDict = departments.ToDictionary(d => d.Name.ToLower(), d => d.Id);

                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var options = new TypeConverterOptions { Formats = new[] { "0.0", "0.00" } };
                    csv.Context.TypeConverterOptionsCache.AddOptions<double>(options);
                    var records = csv.GetRecords<ResultCsvRecord>().ToList();

                    // Fetch all grades for all departments once
                    var allGrades = await _context.Grades
                        .Where(g => g.Type == "ug" && g.Approved)
                        .ToListAsync();

                    string resultId = Guid.NewGuid().ToString();

                    var results = records.Select(record =>
                    {
                        double totalScore = record.CA + record.Exam;

                        // Find DepartmentId
                        var matchedDepartmentId = departmentDict.TryGetValue(record.Department.ToLower(), out var deptId) ? deptId : 0;

                        // Fetch grade scale for this student's department
                        var departmentGrades = allGrades.Where(g => g.DepartmentId == matchedDepartmentId).ToList();

                        // Determine grade
                        var grade = departmentGrades.FirstOrDefault(g => totalScore >= g.MinScore && totalScore <= g.MaxScore);
                        string gradeName = grade?.GradeName ?? "N/A";

                        return new Result
                        {
                            UploadedBy = uploader,
                            LevelId = levelId,
                            DepartmentId = matchedDepartmentId,
                            DepartmentName = record.Department,
                            ResultId = resultId,
                            CourseId = courseId,
                            Session = sessionId,
                            Semester = semesterId,
                            StudentId = record.MatNo,
                            StudentName = record.Name,
                            CA = record.CA,
                            Exam = record.Exam,
                            Total = totalScore,
                            Grade = gradeName,
                            IsCO = gradeName == "F",
                            Created = DateTime.Now,
                        };
                    }).ToList();

                    await _context.Results.AddRangeAsync(results);
                    await _context.SaveChangesAsync();

                    var departmentGroups = results
                        .GroupBy(r => r.DepartmentName)
                        .Select(g => new { DepartmentName = g.Key, StudentsCount = g.Count() })
                        .ToList();

                    foreach (var department in departmentGroups)
                    {
                        await SaveDepartmentalBatch(courseId, semesterId, sessionId, department.DepartmentName, department.StudentsCount, userId, resultId);
                    }

                    return new UploadResultResponse
                    {
                        Success = true,
                        Message = $"{results.Count} records uploaded successfully for {courseId}.",
                        Count = results.Count
                    };
                }
            }
            catch (Exception ex)
            {
                return new UploadResultResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message} - Contact ICT for support",
                    Count = 0
                };
            }

        }
        public async Task<UploadResultResponse> AddSingleResult(string studentId, string StudentName, string courseId, int session, int semester, double ca, double exam, int levelId, string uploader, string dptN, int departmentId, string reference)
        {
            try{
                  double totalScore = ca + exam;

                 var gradeScale = await _context.Grades
                        .Where(g => g.Type == "ug" && g.Approved && g.DepartmentId == departmentId)
                        .ToListAsync();

                         // Determine the grade
                        var grade = gradeScale.FirstOrDefault(g => totalScore >= g.MinScore && totalScore <= g.MaxScore);
                        string gradeName = grade?.GradeName ?? "N/A";

                    var result = new Result{
                        ResultId = reference,
                        StudentId = studentId,
                        CourseId = courseId,
                        Session = session,
                        Semester = semester,
                        CA = ca,
                        Exam = exam,
                        Upgrade = 0,
                        Created = DateTime.Now,
                        LevelId = levelId,
                        UploadedBy = uploader,
                        Total = totalScore,
                        Grade = gradeName,
                        IsCO = gradeName == "F",
                        DepartmentName = dptN,
                        StudentName = StudentName
                };
                        await _context.Results.AddAsync(result);
                        await _context.SaveChangesAsync();
                        return new UploadResultResponse
                    {
                        Success = true,
                        Message = $"A record for {StudentName} has been uploadeded successfully for {courseId}.",
                    };
            }catch(Exception ex){
                return new UploadResultResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message} - Contact ICT for support",
                    Count = 0
                };
            }
        }

        public async Task<string> SaveDepartmentalBatch(string courseId, int semesterId, int sessionId, string departmentName, int studentsCount, string userId, string resultId)
        {
            try
            {
                // Create a new DepartmentBatch object with the provided parameters
                var departmentBatch = new DepartmentBatch
                {
                    User = userId,
                    NoOfStudents = studentsCount,
                    CourseId = courseId,
                    Semester = semesterId,
                    Session = sessionId,
                    ResultId = resultId,
                    DepartmentName = departmentName,
                    LecturerStatus = "Pending",
                    HODStatus = "Pending",
                    DeanStatus = "Pending",
                    CreatedAt = DateTime.Now
                    // Set other properties as needed
                };

                await _context.DepartmentBatches.AddAsync(departmentBatch);
                await _context.SaveChangesAsync();

                return "Record updated successfully.";
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a logging framework here)
                // For now, we'll just return an error message
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<string> SaveFacultyBatch(int semesterId, int sessionId, int departmentId, string departmentName, int facultyId)
        {
            try
            {
                // Create a new DepartmentBatch object with the provided parameters
                var facultyBatch = new FacultyBatch
                {
                    Semester = semesterId,
                    Session = sessionId,
                    DepartmentId = departmentId,
                    DepartmentName = departmentName,
                    Faculty = facultyId,
                    Status = "Pending",
                    Registry = "Pending",
                    CreatedAt =  DateTime.Now
                    // Set other properties as needed
                };

                // Add the new DepartmentBatch to the context
                await _context.FacultyBatches.AddAsync(facultyBatch);

                // Save changes to the database
                await _context.SaveChangesAsync();

                return "Record updated successfully.";
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a logging framework here)
                // For now, we'll just return an error message
                return $"An error occurred: {ex.Message}";
            }

           
        }

        public async Task<string> ApproveOrDecline(string course, int session, string status, string who, string dpt)
        {
            var departmentBatch = await _context.DepartmentBatches
                .FirstOrDefaultAsync(x => x.CourseId == course && x.Session == session && x.DepartmentName == dpt);

            if (departmentBatch == null)
            {
                return "Can't find result for the department";
            }
            if (who == "hod")
            {
                departmentBatch.HODStatus = status;
            }else if(who == "dean"){
                departmentBatch.DeanStatus = status;
            }else if(who == "lecturer"){
                departmentBatch.LecturerStatus = status;
            }
            
            // var hod = await _context.Users.FirstOrDefaultAsync(x => x.DepartmentId == departmentBatch.DepartmentId && );
            await _context.SaveChangesAsync(); // Save the updated status to the database

            return "Done";
        }

        public async Task<string> DeanApproveOrDecline(int dptId, int session, string status)
        {
            var facultyBatch = await _context.FacultyBatches
                .Where(x => x.DepartmentId == dptId && x.Session == session).ToListAsync();

            if (facultyBatch == null)
            {
                return "Can't find result for the department";
            }
            foreach (var faculty in facultyBatch){
                faculty.Status = status;
            }
            await _context.SaveChangesAsync(); // Save the updated status to the database

            return "Done";
        }
        
       public async Task<string> UpgradeBulkResult(string course, int session, int score, int departmentId)
        {
            try
            {
                // Fetch the results to be upgraded
                var resultsToUpgrade = await _context.Results
                    .Where(x => x.CourseId == course && x.Session == session)
                    .ToListAsync();

                // Check if any record's total score will exceed 100 after the update
                foreach (var record in resultsToUpgrade)
                {
                    double newTotal = record.CA + record.Exam + score; // Calculate the new total
                    if (newTotal > 100)
                    {
                        return "Updating the score would cause a total score to exceed 100 for one or more records.";
                    }
                }

                // Use a transaction to ensure atomicity
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Update the scores
                        foreach (var record in resultsToUpgrade)
                        {
                            record.Upgrade = score; // Update the upgrade score
                        }

                        // Save the changes to the database
                        await _context.SaveChangesAsync();

                        // Fetch the grade scale for the department
                        var gradeScale = await _context.Grades
                            .Where(g => g.Type == "ug" && g.DepartmentId == departmentId && g.Approved)
                            .ToListAsync();

                        // Update the grades based on the new scores
                        foreach (var record in resultsToUpgrade)
                        {
                            double totalScore = record.CA + record.Exam + record.Upgrade; // Recalculate total score

                            // Determine the grade
                            var grade = gradeScale.FirstOrDefault(g => totalScore >= g.MinScore && totalScore <= g.MaxScore);
                            string gradeName = grade?.GradeName ?? "N/A";
                            record.Total = totalScore;
                            record.Grade = gradeName;
                            record.IsCO = gradeName == "F";
                            record.UpdatedAt = DateTime.Now;
                        }

                        // Save the changes to the database again
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return "Done";
                    }
                    catch (Exception)
                    {
                        // Rollback the transaction in case of an error
                        await transaction.RollbackAsync();
                        throw; // Re-throw the exception to be handled by the outer catch block
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a logging framework here)
                // For now, we'll just return an error message
               return $"An error occurred: {ex.Message}";
            }
        }


        public async Task<string> UpgradeSingleResult(int id, string studentId, double score, int departmentId)
        {
            try
            {
                var resultToUpgrade = await _context.Results
                    .Where(x => x.Id == id && x.StudentId == studentId)
                    .FirstOrDefaultAsync();

                if (resultToUpgrade == null)
                {
                    return "Result not found.";
                }

                double newTotal = resultToUpgrade.CA + resultToUpgrade.Exam + score;
                if (newTotal > 100)
                {
                    return "Updating the score would cause the total score to exceed 100.";
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        resultToUpgrade.Upgrade = score;
                        await _context.SaveChangesAsync();

                         var gradeScale = await _context.Grades
                            .Where(g => g.Type == "ug" && g.DepartmentId == departmentId && g.Approved)
                            .ToListAsync();

                        double totalScore = resultToUpgrade.CA + resultToUpgrade.Exam + resultToUpgrade.Upgrade;
                        var grade = gradeScale.FirstOrDefault(g => totalScore >= g.MinScore && totalScore <= g.MaxScore);
                        resultToUpgrade.Total = totalScore;
                        resultToUpgrade.Grade = grade?.GradeName ?? "N/A";
                        resultToUpgrade.IsCO = resultToUpgrade.Grade == "F";
                        resultToUpgrade.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return "Done"; // ✅ This ensures the controller recognizes success
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }
        public async Task<string> UpdateDptRecord(string courseId, int sessionId, string departmentName, int studentsCount)
        {
            try
            {
                // Create a new DepartmentBatch object with the provided parameters
                var departmentBatch = await _context.DepartmentBatches.FirstOrDefaultAsync(x => x.CourseId == courseId && x.Session == sessionId && x.DepartmentName == departmentName);
                departmentBatch.NoOfStudents += 1;
                await _context.SaveChangesAsync();

                return "Record updated successfully.";
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use a logging framework here)
                // For now, we'll just return an error message
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<string> UpdateAllResultGrades()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // FETCH DEPARTMENTS FROM API
                string apiUrl = $"https://edouniversity.edu.ng/api/v1/departmentsapi";
                string apiKey = Environment.GetEnvironmentVariable("EUI_API_KEY");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                var response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var departments = JsonConvert.DeserializeObject<List<Departments>>(content);

                foreach (var dept in departments)
                {
                    // Get grade scale for department
                    var gradeScale = await _context.Grades
                        .Where(g => g.Type == "ug" && g.DepartmentId == dept.Id && g.Approved)
                        .ToListAsync();

                    if (!gradeScale.Any()) continue;

                    // Get all results for department
                    var resultGrades = await _context.Results
                        .Where(r => r.DepartmentId == dept.Id)
                        .ToListAsync();

                    foreach (var result in resultGrades)
                    {
                        double totalScore = result.CA + result.Exam + result.Upgrade;
                        var grade = gradeScale.FirstOrDefault(g => totalScore >= g.MinScore && totalScore <= g.MaxScore);

                        result.Total = totalScore;
                        result.Grade = grade?.GradeName ?? "N/A";
                        result.IsCO = result.Grade == "F";
                        result.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return "Done";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
}

    public class ResultCsvRecord
    {
        public string? MatNo { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public double CA { get; set; }
        public double Exam { get; set; }
    }

    public class ResultUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Count { get; set; }
    }

}