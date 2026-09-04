using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;
using Nirvachak_AI.Domain.Enums;
using Nirvachak_AI.Infrastructure.Data;

namespace Nirvachak_AI.Infrastructure.Services;

public static class SeedService
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // ── Safety log: print user count so it is visible in Railway deploy logs ──
        var totalUsers = await userManager.Users.CountAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SEED] Database connected. Total users in DB: {totalUsers}");
        Console.ResetColor();

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Seed SuperAdmin (platform-level, no constituency) ─────────────────
        if (!await userManager.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
        {
            var superAdmin = new AppUser
            {
                UserName       = "superadmin@nirvachak.ai",
                Email          = "superadmin@nirvachak.ai",
                FullName       = "Platform Super Admin",
                Role           = UserRole.SuperAdmin,
                ConstituencyId = null,
                EmailConfirmed = true,
                IsActive       = true
            };
            var r = await userManager.CreateAsync(superAdmin, "SuperAdmin@123");
            if (r.Succeeded) await userManager.AddToRoleAsync(superAdmin, nameof(UserRole.SuperAdmin));
        }

        if (!db.Constituencies.Any())
        {
            var constituency = new Constituency
            {
                Name = "Pune Cantonment",
                Code = "AC-168",
                ElectionType = ElectionType.MLA,
                State = "Maharashtra",
                District = "Pune",
                CandidateName = "Demo Candidate",
                PartyName = "Demo Party",
                ElectionDate = new DateTime(2025, 11, 15)
            };
            db.Constituencies.Add(constituency);
            await db.SaveChangesAsync();

            for (int i = 1; i <= 8; i++)
            {
                db.Booths.Add(new Booth
                {
                    BoothNumber = i,
                    BoothName = $"Booth {i} - Primary School Ward {i}",
                    Address = $"Ward {i}, Pune Cantonment",
                    WardNumber = i.ToString(),
                    ConstituencyId = constituency.Id,
                    TotalVoters = 400 + (i * 15)
                });
                db.Wards.Add(new Ward
                {
                    WardNumber = i.ToString(),
                    WardName = $"Ward {i} - {new[] { "Gandhi Nagar", "Nehru Vihar", "Shivaji Park", "Ambedkar Colony", "Tilak Nagar", "Subhash Chowk", "Patel Wadi", "Laxmi Nagar" }[i - 1]}",
                    Description = $"Ward {i} of Pune Cantonment constituency",
                    ConstituencyId = constituency.Id
                });
            }
            await db.SaveChangesAsync();

            // Seed sample voters for demo
            var rnd = new Random(42);
            var names = new[] { "Rajesh Kumar", "Sunita Sharma", "Amit Patel", "Priya Singh", "Vijay Rao",
                "Meena Joshi", "Sanjay Gupta", "Kavitha Nair", "Dinesh Patil", "Anita More" };
            var voters = new List<Voter>();
            for (int b = 1; b <= 4; b++)
            {
                for (int s = 1; s <= 30; s++)
                {
                    voters.Add(new Voter
                    {
                        VoterId = $"MH{b:D2}{s:D4}",
                        Name = names[rnd.Next(names.Length)] + $" {s}",
                        Age = 20 + rnd.Next(55),
                        Gender = rnd.Next(2) == 0 ? "M" : "F",
                        Address = $"House {s}, Ward {b}, Pune",
                        BoothNumber = b,
                        WardNumber = b.ToString(),
                        PannaNumber = ((s - 1) / 10 + 1).ToString(),
                        SerialNumber = s,
                        ConstituencyId = constituency.Id,
                        Sentiment = (VoterSentiment)rnd.Next(5)
                    });
                }
            }
            await db.Voters.AddRangeAsync(voters);
            await db.SaveChangesAsync();
        }

        // Seed each default account independently — never use Users.Any() as the guard
        // because SuperAdmin is already seeded above, making Users.Any() always true.
        // IMPORTANT: Only seed demo accounts if no real (non-demo) users exist yet.
        // This prevents overwriting or interfering with users created in the Admin panel.
        var demoEmails = new[] { "admin@election.com", "manager@election.com", "worker@election.com", "superadmin@nirvachak.ai" };
        var hasRealUsers = await userManager.Users
            .AnyAsync(u => !demoEmails.Contains(u.Email));

        if (hasRealUsers)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SEED] Real users detected — skipping demo user seeding to protect existing accounts.");
            Console.ResetColor();
        }

        if (db.Constituencies.Any())
        {
            var constituency = db.Constituencies.First();

            if (!hasRealUsers)
            {
            if (await userManager.FindByEmailAsync("admin@election.com") == null)
            {
                var admin = new AppUser
                {
                    UserName       = "admin@election.com",
                    Email          = "admin@election.com",
                    FullName       = "System Administrator",
                    Role           = UserRole.Admin,
                    ConstituencyId = constituency.Id,
                    EmailConfirmed = true,
                    IsActive       = true
                };
                var r = await userManager.CreateAsync(admin, "Admin@123");
                if (r.Succeeded) await userManager.AddToRoleAsync(admin, nameof(UserRole.Admin));
            }

            if (await userManager.FindByEmailAsync("manager@election.com") == null)
            {
                var manager = new AppUser
                {
                    UserName       = "manager@election.com",
                    Email          = "manager@election.com",
                    FullName       = "Campaign Manager",
                    Role           = UserRole.CampaignManager,
                    ConstituencyId = constituency.Id,
                    EmailConfirmed = true,
                    IsActive       = true
                };
                var r2 = await userManager.CreateAsync(manager, "Manager@123");
                if (r2.Succeeded) await userManager.AddToRoleAsync(manager, nameof(UserRole.CampaignManager));
            }

            if (await userManager.FindByEmailAsync("worker@election.com") == null)
            {
                var worker = new AppUser
                {
                    UserName             = "worker@election.com",
                    Email                = "worker@election.com",
                    FullName             = "Field Worker 1",
                    Role                 = UserRole.FieldWorker,
                    ConstituencyId       = constituency.Id,
                    AssignedBoothNumbers = "1,2",
                    EmailConfirmed       = true,
                    IsActive             = true
                };
                var r3 = await userManager.CreateAsync(worker, "Worker@123");
                if (r3.Succeeded) await userManager.AddToRoleAsync(worker, nameof(UserRole.FieldWorker));
            }
            } // end !hasRealUsers
        }

        // ── Transport Vehicles ────────────────────────────────────────────────
        if (!db.TransportVehicles.Any())
        {
            var constituency = db.Constituencies.First();
            db.TransportVehicles.AddRange(
                new TransportVehicle { DriverName = "Ravi Shinde",    DriverPhone = "9876500003", VehicleNumber = "MH12AB1001", VehicleType = "Van",  Capacity = 7, BoothNumber = 1, IsAvailable = true, Notes = "Available from 7 AM on election day.",   ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Suresh Mane",    DriverPhone = "9876500009", VehicleNumber = "MH12CD2002", VehicleType = "Car",  Capacity = 4, BoothNumber = 2, IsAvailable = true, Notes = "Covers Ward 1 & 2.",                    ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Ganesh Thorat",  DriverPhone = "9823400020", VehicleNumber = "MH12EF3003", VehicleType = "Auto", Capacity = 3, BoothNumber = 3, IsAvailable = true, Notes = "Handles narrow lanes in Ward 3.",        ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Pradeep Kamble", DriverPhone = "9823400021", VehicleNumber = "MH12GH4004", VehicleType = "Van",  Capacity = 7, BoothNumber = 4, IsAvailable = true, Notes = "Dedicated to Ambedkar Colony voters.",  ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Rajesh Patil",   DriverPhone = "9823400022", VehicleNumber = "MH12IJ5005", VehicleType = "Car",  Capacity = 4, BoothNumber = 5, IsAvailable = true, Notes = "Available all day, Tilak Nagar area.",  ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Mohan Yadav",    DriverPhone = "9823400023", VehicleNumber = "MH12KL6006", VehicleType = "Bus",  Capacity = 20, BoothNumber = 6, IsAvailable = true, Notes = "Mini-bus for bulk voter transport.",   ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Santosh Nair",   DriverPhone = "9823400024", VehicleNumber = "MH12MN7007", VehicleType = "Van",  Capacity = 7, BoothNumber = 7, IsAvailable = true, Notes = "Patel Wadi & surrounding area.",        ConstituencyId = constituency.Id },
                new TransportVehicle { DriverName = "Dinesh Salve",   DriverPhone = "9823400025", VehicleNumber = "MH12OP8008", VehicleType = "Car",  Capacity = 4, BoothNumber = 8, IsAvailable = true, Notes = "Laxmi Nagar — senior citizen priority.", ConstituencyId = constituency.Id }
            );
            await db.SaveChangesAsync();
        }

        // ?? Campaign Events ??????????????????????????????????????????????????
        if (!db.CampaignEvents.Any())
        {
            var constituency = db.Constituencies.First();
            var now = DateTime.Now;
            db.CampaignEvents.AddRange(
                new CampaignEvent { Title = "Gandhi Nagar Padyatra", EventType = CampaignEventType.DoorToDoor, Description = "Door-to-door campaign covering all lanes of Gandhi Nagar ward.", Location = "Gandhi Nagar, Ward 1", ScheduledAt = now.AddDays(-20), ActualAttendance = 38, ExpectedAttendance = 40, TargetWards = "1", IsCompleted = true, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Nehru Vihar Public Rally", EventType = CampaignEventType.Rally, Description = "Large public rally addressing infrastructure & water supply issues.", Location = "Nehru Vihar Ground, Ward 2", ScheduledAt = now.AddDays(-15), ActualAttendance = 520, ExpectedAttendance = 500, TargetWards = "2", IsCompleted = true, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Youth Connect � Shivaji Park", EventType = CampaignEventType.SmallMeeting, Description = "Interactive session with first-time voters and youth leaders.", Location = "Shivaji Park Community Hall, Ward 3", ScheduledAt = now.AddDays(-12), ActualAttendance = 65, ExpectedAttendance = 75, TargetWards = "3", IsCompleted = true, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Senior Citizens Samvad", EventType = CampaignEventType.SmallMeeting, Description = "Grievance-listening session for senior citizens.", Location = "Ambedkar Colony Chowk, Ward 4", ScheduledAt = now.AddDays(-10), ActualAttendance = 42, ExpectedAttendance = 50, TargetWards = "4", IsCompleted = true, OrganizedByName = "Field Worker 1", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Phone Banking Drive � Booths 1-3", EventType = CampaignEventType.PhoneCall, Description = "Volunteer phone-calling drive targeting undecided voters.", Location = "Campaign Office", ScheduledAt = now.AddDays(-7), ActualAttendance = 12, ExpectedAttendance = 15, TargetBoothNumbers = "1,2,3", IsCompleted = true, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Tilak Nagar Nukkad Natak", EventType = CampaignEventType.Other, Description = "Street play highlighting candidate's development agenda.", Location = "Tilak Nagar Square, Ward 5", ScheduledAt = now.AddDays(-4), ActualAttendance = 180, ExpectedAttendance = 150, TargetWards = "5", IsCompleted = true, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Subhash Chowk Door-to-Door", EventType = CampaignEventType.DoorToDoor, Description = "Voter connect initiative covering 200 households.", Location = "Subhash Chowk, Ward 6", ScheduledAt = now.AddDays(2), ExpectedAttendance = 30, TargetWards = "6", IsCompleted = false, OrganizedByName = "Field Worker 1", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Patel Wadi Mega Rally", EventType = CampaignEventType.LargeMeeting, Description = "Grand pre-election rally with party leaders.", Location = "Patel Wadi Sports Ground, Ward 7", ScheduledAt = now.AddDays(5), ExpectedAttendance = 1200, TargetWards = "7", IsCompleted = false, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Women Voters Outreach � Laxmi Nagar", EventType = CampaignEventType.SmallMeeting, Description = "Special session for women voters on safety and empowerment schemes.", Location = "Laxmi Nagar School, Ward 8", ScheduledAt = now.AddDays(8), ExpectedAttendance = 90, TargetWards = "8", IsCompleted = false, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id },
                new CampaignEvent { Title = "Constituency-Wide Phone Marathon", EventType = CampaignEventType.PhoneCall, Description = "Final voter reminder calls across all booths.", Location = "Campaign Office", ScheduledAt = now.AddDays(12), ExpectedAttendance = 20, TargetBoothNumbers = "1,2,3,4,5,6,7,8", IsCompleted = false, OrganizedByName = "Campaign Manager", ConstituencyId = constituency.Id }
            );
            await db.SaveChangesAsync();
        }

        // ?? Volunteers ???????????????????????????????????????????????????????
        if (!db.Volunteers.Any())
        {
            var constituency = db.Constituencies.First();
            db.Volunteers.AddRange(
                new Volunteer { Name = "Arjun Deshmukh",   Phone = "9876500001", Email = "arjun.d@mail.com",   Address = "12, Gandhi Nagar, Ward 1",      AssignedArea = "Ward 1",   AssignedBoothNumbers = "1",   Task = VolunteerTask.BoothManagement, IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-30), Notes = "Experienced booth in-charge.",              ConstituencyId = constituency.Id },
                new Volunteer { Name = "Sneha Kulkarni",   Phone = "9876500002", Email = "sneha.k@mail.com",   Address = "45, Nehru Vihar, Ward 2",       AssignedArea = "Ward 2",   AssignedBoothNumbers = "2",   Task = VolunteerTask.VoterOutreach,   IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-28), Notes = "Fluent in Marathi and Hindi.",              ConstituencyId = constituency.Id },
                new Volunteer { Name = "Ravi Shinde",      Phone = "9876500003",                               Address = "88, Shivaji Park, Ward 3",      AssignedArea = "Ward 3",   AssignedBoothNumbers = "3",   Task = VolunteerTask.Transport,       IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-25), Notes = "Owns a 7-seater vehicle.",                  ConstituencyId = constituency.Id },
                new Volunteer { Name = "Pooja Waghmare",   Phone = "9876500004", Email = "pooja.w@mail.com",   Address = "7, Ambedkar Colony, Ward 4",    AssignedArea = "Ward 4",   AssignedBoothNumbers = "4",   Task = VolunteerTask.DataEntry,       IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-22), Notes = "IT graduate, handles voter list updates.",  ConstituencyId = constituency.Id },
                new Volunteer { Name = "Nitin Jadhav",     Phone = "9876500005",                               Address = "33, Tilak Nagar, Ward 5",       AssignedArea = "Ward 5",   AssignedBoothNumbers = "5",   Task = VolunteerTask.BoothManagement, IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-20), Notes = "Party worker for 8 years.",                 ConstituencyId = constituency.Id },
                new Volunteer { Name = "Kavya Iyer",       Phone = "9876500006", Email = "kavya.i@mail.com",   Address = "22, Subhash Chowk, Ward 6",     AssignedArea = "Ward 6",   AssignedBoothNumbers = "6",   Task = VolunteerTask.Communication,   IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-18), Notes = "Handles social media updates.",             ConstituencyId = constituency.Id },
                new Volunteer { Name = "Rahul Borse",      Phone = "9876500007",                               Address = "61, Patel Wadi, Ward 7",        AssignedArea = "Ward 7",   AssignedBoothNumbers = "7",   Task = VolunteerTask.VoterOutreach,   IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-15), Notes = "Active in local sports club.",              ConstituencyId = constituency.Id },
                new Volunteer { Name = "Deepa Sawant",     Phone = "9876500008", Email = "deepa.s@mail.com",   Address = "14, Laxmi Nagar, Ward 8",       AssignedArea = "Ward 8",   AssignedBoothNumbers = "8",   Task = VolunteerTask.BoothManagement, IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-12), Notes = "Women's group coordinator.",               ConstituencyId = constituency.Id },
                new Volunteer { Name = "Suresh Mane",      Phone = "9876500009",                               Address = "77, Gandhi Nagar, Ward 1",      AssignedArea = "Ward 1,2", AssignedBoothNumbers = "1,2", Task = VolunteerTask.Transport,       IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-10), Notes = "Coordinates vehicle pooling.",              ConstituencyId = constituency.Id },
                new Volunteer { Name = "Anita Pawar",      Phone = "9876500010", Email = "anita.p@mail.com",   Address = "5, Nehru Vihar, Ward 2",        AssignedArea = "Ward 2,3", AssignedBoothNumbers = "2,3", Task = VolunteerTask.DataEntry,       IsActive = false, RegisteredAt = DateTime.UtcNow.AddDays(-8),  Notes = "On medical leave till further notice.",     ConstituencyId = constituency.Id },
                new Volunteer { Name = "Vikram Salunkhe",  Phone = "9876500011",                               Address = "99, Shivaji Park, Ward 3",      AssignedArea = "Ward 3,4", AssignedBoothNumbers = "3,4", Task = VolunteerTask.Other,           IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-5),  Notes = "Logistics and supply management.",          ConstituencyId = constituency.Id },
                new Volunteer { Name = "Manisha Thorat",   Phone = "9876500012", Email = "manisha.t@mail.com", Address = "38, Ambedkar Colony, Ward 4",   AssignedArea = "Ward 4,5", AssignedBoothNumbers = "4,5", Task = VolunteerTask.VoterOutreach,   IsActive = true,  RegisteredAt = DateTime.UtcNow.AddDays(-3),  Notes = "Conducts door-to-door voter education.",   ConstituencyId = constituency.Id }
            );
            await db.SaveChangesAsync();
        }

        // ?? Grievances ???????????????????????????????????????????????????????
        if (!db.Grievances.Any())
        {
            var constituency = db.Constituencies.First();
            var now = DateTime.UtcNow;
            db.Grievances.AddRange(
                new Grievance { Title = "Broken Road Near School", Description = "The main road in front of the primary school in Ward 1 has large potholes causing accidents, especially during rainy season. Needs immediate repair.", ReportedBy = "Mahesh Tiwari", ReporterPhone = "9900011001", Ward = "1", BoothNumber = 1, Priority = GrievancePriority.High, Status = GrievanceStatus.Open, Location = "Gandhi Nagar, Ward 1", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-22) },
                new Grievance { Title = "No Street Lights on Main Road", Description = "Street lights on the stretch from Nehru Vihar to Subhash Chowk have been non-functional for over a month. Residents fear for safety at night.", ReportedBy = "Lata Borkar", ReporterPhone = "9900011002", Ward = "2", BoothNumber = 2, Priority = GrievancePriority.Medium, Status = GrievanceStatus.InProgress, AssignedToName = "Campaign Manager", Location = "Nehru Vihar, Ward 2", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-18) },
                new Grievance { Title = "Water Supply Disruption", Description = "Water supply has been irregular for three weeks in Ward 3. Residents in the eastern part receive water only twice a week, which is insufficient.", ReportedBy = "Ganesh Parab", ReporterPhone = "9900011003", Ward = "3", BoothNumber = 3, Priority = GrievancePriority.Critical, Status = GrievanceStatus.Open, Location = "Shivaji Park, Ward 3", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-16) },
                new Grievance { Title = "Garbage Not Collected for 2 Weeks", Description = "Garbage collection van has not visited Ward 4 for over two weeks. Waste is piling up near Ambedkar Colony chowk, causing hygiene issues.", ReportedBy = "Sunita Rao", ReporterPhone = "9900011004", Ward = "4", BoothNumber = 4, Priority = GrievancePriority.High, Status = GrievanceStatus.Resolved, AssignedToName = "Field Worker 1", ResolutionNotes = "Escalated to municipal authority. Collection resumed from 20th.", Location = "Ambedkar Colony, Ward 4", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-14), ResolvedAt = now.AddDays(-5) },
                new Grievance { Title = "Illegal Construction Blocking Road", Description = "An unauthorized structure is being erected at the entrance of Tilak Nagar, blocking vehicular movement. Local residents have complained multiple times.", ReportedBy = "Ranjit More", ReporterPhone = "9900011005", Ward = "5", BoothNumber = 5, Priority = GrievancePriority.High, Status = GrievanceStatus.Open, Location = "Tilak Nagar, Ward 5", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-12) },
                new Grievance { Title = "Stray Dogs Menace Near Booth 6", Description = "A large pack of stray dogs near the community park in Ward 6 is terrorising children and elderly residents. Animal control intervention urgently required.", ReportedBy = "Fatima Sheikh", ReporterPhone = "9900011006", Ward = "6", BoothNumber = 6, Priority = GrievancePriority.Medium, Status = GrievanceStatus.InProgress, AssignedToName = "Campaign Manager", Location = "Subhash Chowk, Ward 6", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-10) },
                new Grievance { Title = "Drainage Overflow in Ward 7", Description = "The drainage system near Patel Wadi overflows during heavy rains, flooding nearby houses. Repeated complaints to the civic body have gone unheeded.", ReportedBy = "Harish Nair", ReporterPhone = "9900011007", Ward = "7", BoothNumber = 7, Priority = GrievancePriority.Critical, Status = GrievanceStatus.Open, Location = "Patel Wadi, Ward 7", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-9) },
                new Grievance { Title = "Election Voter List Correction Needed", Description = "Several registered voters in Ward 8 (Laxmi Nagar) find their names missing from the updated voter list. Requires urgent correction before election day.", ReportedBy = "Priti Gaikwad", ReporterPhone = "9900011008", Ward = "8", BoothNumber = 8, Priority = GrievancePriority.Critical, Status = GrievanceStatus.InProgress, AssignedToName = "Campaign Manager", Location = "Laxmi Nagar, Ward 8", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-8) },
                new Grievance { Title = "Government School Lacks Drinking Water", Description = "The government primary school in Ward 1 does not have a functional water purifier. Students are forced to drink tap water, causing health concerns.", ReportedBy = "Sanjay Kale", ReporterPhone = "9900011009", Ward = "1", BoothNumber = 1, Priority = GrievancePriority.Medium, Status = GrievanceStatus.Resolved, AssignedToName = "Field Worker 1", ResolutionNotes = "RO unit installed by constituency fund on request.", Location = "Gandhi Nagar School, Ward 1", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-20), ResolvedAt = now.AddDays(-6) },
                new Grievance { Title = "Encroachment on Public Park", Description = "A portion of the community park in Ward 2 has been encroached upon for commercial use. Residents want the encroachment removed and the park restored.", ReportedBy = "Rekha Chavan", ReporterPhone = "9900011010", Ward = "2", BoothNumber = 2, Priority = GrievancePriority.Low, Status = GrievanceStatus.Open, Location = "Nehru Vihar Park, Ward 2", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-7) },
                new Grievance { Title = "Bus Stop in Dilapidated Condition", Description = "The bus stop shelter near Booth 3 in Shivaji Park is in very poor condition. The roof leaks, and the seating is broken. Commuters request refurbishment.", ReportedBy = "Abhay Patil", ReporterPhone = "9900011011", Ward = "3", BoothNumber = 3, Priority = GrievancePriority.Low, Status = GrievanceStatus.Open, Location = "Shivaji Park Bus Stop, Ward 3", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-6) },
                new Grievance { Title = "Electricity Outage Lasting 8+ Hours", Description = "Ward 5 experienced electricity outages of 8 to 10 hours consecutively for the past week. Residents with medical equipment are severely affected.", ReportedBy = "Neha Deshpande", ReporterPhone = "9900011012", Ward = "5", BoothNumber = 5, Priority = GrievancePriority.High, Status = GrievanceStatus.Resolved, AssignedToName = "Campaign Manager", ResolutionNotes = "Reported to electricity board; transformer repaired on priority.", Location = "Tilak Nagar, Ward 5", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-15), ResolvedAt = now.AddDays(-4) },
                new Grievance { Title = "Public Toilet Non-Functional Near Booth 6", Description = "The only public toilet near the booth 6 polling station in Ward 6 has been locked and non-functional for a month. Residents are inconvenienced.", ReportedBy = "Tushar Ghosh", ReporterPhone = "9900011013", Ward = "6", BoothNumber = 6, Priority = GrievancePriority.Medium, Status = GrievanceStatus.Open, Location = "Subhash Chowk, Ward 6", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-5) },
                new Grievance { Title = "Road Widening Project Stalled", Description = "The road widening project at the Ward 7�8 boundary announced six months ago has been stalled. Heavy traffic is causing daily jams and accidents.", ReportedBy = "Smita Londhe", ReporterPhone = "9900011014", Ward = "7", BoothNumber = 7, Priority = GrievancePriority.High, Status = GrievanceStatus.InProgress, AssignedToName = "Campaign Manager", Location = "Patel Wadi�Laxmi Nagar boundary", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-4) },
                new Grievance { Title = "Senior Citizen Pension Not Disbursed", Description = "Multiple senior citizens in Ward 8 have not received their monthly pension for the past two months. They request urgent follow-up with the concerned office.", ReportedBy = "Vinod Kamble", ReporterPhone = "9900011015", Ward = "8", BoothNumber = 8, Priority = GrievancePriority.High, Status = GrievanceStatus.Open, Location = "Laxmi Nagar, Ward 8", ConstituencyId = constituency.Id, ReportedAt = now.AddDays(-3) }
            );
            await db.SaveChangesAsync();
        }

        // ?? Surveys & Feedback ???????????????????????????????????????????????
        if (!db.Surveys.Any())
        {
            var constituency = db.Constituencies.First();
            var rnd = new Random(7);

            var survey1 = new Survey
            {
                Title = "Candidate Awareness Survey",
                Description = "How aware are voters about the candidate's background, vision, and election promises?",
                Category = SurveyCategory.CandidateAwareness,
                ConstituencyId = constituency.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-18)
            };
            var survey2 = new Survey
            {
                Title = "Local Development Issues",
                Description = "Identify the top infrastructure and development concerns in each ward.",
                Category = SurveyCategory.LocalIssues,
                ConstituencyId = constituency.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            };
            var survey3 = new Survey
            {
                Title = "Party Support & General Opinion",
                Description = "Gauge voter sentiment towards the party and the current political climate.",
                Category = SurveyCategory.GeneralOpinion,
                ConstituencyId = constituency.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            var respondents = new[]
            {
                ("Mahesh Tiwari",   "9900011001", "Ward 1", 1),
                ("Lata Borkar",     "9900011002", "Ward 1", 1),
                ("Ganesh Parab",    "9900011003", "Ward 2", 2),
                ("Sunita Rao",      "9900011004", "Ward 2", 2),
                ("Ranjit More",     "9900011005", "Ward 3", 3),
                ("Fatima Sheikh",   "9900011006", "Ward 3", 3),
                ("Harish Nair",     "9900011007", "Ward 4", 4),
                ("Priti Gaikwad",   "9900011008", "Ward 4", 4),
                ("Sanjay Kale",     "9900011009", "Ward 5", 5),
                ("Rekha Chavan",    "9900011010", "Ward 5", 5),
                ("Abhay Patil",     "9900011011", "Ward 6", 6),
                ("Neha Deshpande",  "9900011012", "Ward 6", 6),
                ("Tushar Ghosh",    "9900011013", "Ward 7", 7),
                ("Smita Londhe",    "9900011014", "Ward 7", 7),
                ("Vinod Kamble",    "9900011015", "Ward 8", 8),
            };

            var feedbackPool = new[]
            {
                "Very impressed with the candidate's roadmap for our area.",
                "Roads and drainage need urgent attention.",
                "Need more public parks and street lights.",
                "Happy with the outreach efforts; more visits needed.",
                "Employment for youth is our top priority.",
                "Water supply is erratic, especially in summer.",
                "Candidate has a good track record in public service.",
                "Garbage collection is a major issue in our ward.",
                "We want better government schools and hospitals.",
                "Strong support from our community for the party.",
                "Neutral � we'll decide based on manifesto.",
                "Local leaders are unresponsive to daily complaints.",
                "Great initiative; we hope promises are kept.",
                "Traffic management needs significant improvement.",
                "Women's safety must be addressed as a priority."
            };

            survey1.Responses = respondents.Take(12).Select((r, i) => new SurveyResponse
            {
                RespondentName = r.Item1, RespondentPhone = r.Item2, Ward = r.Item3, BoothNumber = r.Item4,
                Rating = 3 + (i % 3), Feedback = feedbackPool[i % feedbackPool.Length],
                RespondedAt = DateTime.UtcNow.AddDays(-15 + i)
            }).ToList();

            survey2.Responses = respondents.Skip(2).Take(11).Select((r, i) => new SurveyResponse
            {
                RespondentName = r.Item1, RespondentPhone = r.Item2, Ward = r.Item3, BoothNumber = r.Item4,
                Rating = 2 + (i % 4), Feedback = feedbackPool[(i + 3) % feedbackPool.Length],
                RespondedAt = DateTime.UtcNow.AddDays(-9 + i)
            }).ToList();

            survey3.Responses = respondents.Skip(4).Take(10).Select((r, i) => new SurveyResponse
            {
                RespondentName = r.Item1, RespondentPhone = r.Item2, Ward = r.Item3, BoothNumber = r.Item4,
                Rating = 1 + (i % 5), Feedback = feedbackPool[(i + 6) % feedbackPool.Length],
                RespondedAt = DateTime.UtcNow.AddDays(-4 + i)
            }).ToList();

        db.Surveys.AddRange(survey1, survey2, survey3);
            await db.SaveChangesAsync();
        }

        // ?? Announcements (seed test data) ??????????????????????????????????????
        if (!db.Announcements.Any())
        {
            var constituency = db.Constituencies.First();
            var adminUser   = await userManager.FindByEmailAsync("admin@election.com");
            var managerUser = await userManager.FindByEmailAsync("manager@election.com");
            var workerUser  = await userManager.FindByEmailAsync("worker@election.com");
            var now         = DateTime.UtcNow;

            db.Announcements.AddRange(

                // 1. CRITICAL ALERT — pinned, no expiry, requires ack
                new Announcement
                {
                    Title                  = "⚠️ EVM Malfunction Reported at Booth 3",
                    Body                   = "A technical issue has been reported with the EVM unit at Booth 3 (Shivaji Park). All booth agents and field workers in Ward 3 must halt operations and await instructions from the returning officer. Do NOT allow any voting till further notice.",
                    Category               = AnnouncementCategory.CriticalAlert,
                    CreatedByUserId        = managerUser?.Id ?? "system",
                    CreatedByName          = managerUser?.FullName ?? "Campaign Manager",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "All",
                    IsPinned               = true,
                    RequiresAcknowledgement = true,
                    IsActive               = true,
                    CreatedAt              = now.AddHours(-2)
                },

                // 2. EC COMPLIANCE NOTICE — requires ack, targeted at manager + candidate
                new Announcement
                {
                    Title                  = "EC Expense Cap Updated — ₹40 Lakh Limit Effective Immediately",
                    Body                   = "The Election Commission has revised the election expenditure ceiling for MLA constituencies to ₹40,00,000. All campaign expenses from today onwards must be within this limit. Each expense entry must be documented with receipts. Non-compliance will lead to disqualification.\n\nPlease acknowledge this notice to confirm you have read and understood the new limit.",
                    Category               = AnnouncementCategory.ECComplianceNotice,
                    CreatedByUserId        = adminUser?.Id ?? "system",
                    CreatedByName          = adminUser?.FullName ?? "System Administrator",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "CampaignManager,Candidate",
                    IsPinned               = false,
                    RequiresAcknowledgement = true,
                    IsActive               = true,
                    CreatedAt              = now.AddDays(-1)
                },

                // 3. CAMPAIGN ANNOUNCEMENT — all roles, expires after rally
                new Announcement
                {
                    Title                  = "Patel Wadi Mega Rally — Final Schedule Confirmed",
                    Body                   = "The grand public rally at Patel Wadi Sports Ground (Ward 7) is confirmed for this Saturday at 4:00 PM.\n\n📍 Venue: Patel Wadi Sports Ground, Ward 7\n🕓 Time: 4:00 PM sharp\n🚌 Volunteer transport leaves from Campaign Office at 3:15 PM\n\nAll field workers and booth agents must report by 3:30 PM for crowd management duties. Wear your campaign vests.",
                    Category               = AnnouncementCategory.CampaignAnnouncement,
                    CreatedByUserId        = managerUser?.Id ?? "system",
                    CreatedByName          = managerUser?.FullName ?? "Campaign Manager",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "All",
                    IsPinned               = false,
                    RequiresAcknowledgement = false,
                    IsActive               = true,
                    ExpiresAt              = now.AddDays(6),
                    CreatedAt              = now.AddDays(-2)
                },

                // 4. DAILY BRIEFING — field workers + booth agents
                new Announcement
                {
                    Title                  = "Morning Briefing — Today's Voter Contact Targets",
                    Body                   = "Good morning team! Here are today's targets:\n\n🎯 Ward 5 (Tilak Nagar): 120 voter contacts\n🎯 Ward 6 (Subhash Chowk): 95 voter contacts\n🎯 Ward 7 (Patel Wadi): 85 voter contacts\n\nPriority pannas: 3A, 3B, and 4C in Ward 5. Focus on undecided and floating voters. Update sentiments in the app after each visit.\n\nDaily call at 6:00 PM on WhatsApp group for status update.",
                    Category               = AnnouncementCategory.DailyBriefing,
                    CreatedByUserId        = managerUser?.Id ?? "system",
                    CreatedByName          = managerUser?.FullName ?? "Campaign Manager",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "FieldWorker,BoothAgent",
                    IsPinned               = false,
                    RequiresAcknowledgement = false,
                    IsActive               = true,
                    ExpiresAt              = now.AddHours(20),
                    CreatedAt              = now.AddHours(-6)
                },

                // 5. MOTIVATION — all roles, from candidate
                new Announcement
                {
                    Title                  = "🏆 We Just Crossed 1,000 Favour Contacts — Thank You!",
                    Body                   = "Dear Team,\n\nI am proud to share that as of this morning, we have successfully contacted and marked over 1,000 voters as 'In Favour' — a milestone that reflects every visit, every call, and every conversation you have had.\n\nYour dedication is what makes this campaign strong. Keep going — election day is near and every single contact counts.\n\nWith gratitude,\nDemo Candidate",
                    Category               = AnnouncementCategory.Motivation,
                    CreatedByUserId        = adminUser?.Id ?? "system",
                    CreatedByName          = "Demo Candidate",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "All",
                    IsPinned               = false,
                    RequiresAcknowledgement = false,
                    IsActive               = true,
                    CreatedAt              = now.AddDays(-3)
                },

                // 6. LIVE DATA NUDGE — auto-generated, all roles
                new Announcement
                {
                    Title                  = "📊 Booth 7 Coverage Dropped Below 30% — Needs Attention",
                    Body                   = "Platform alert: Booth 7 (Patel Wadi) voter contact coverage has dropped to 27% — below the 30% threshold.\n\nOnly 113 of 420 registered voters have been contacted so far. This booth is at risk of low turnout. Please prioritise door-to-door outreach in this area today.\n\nContact the assigned booth agent (Rahul Borse, 9876500007) to coordinate.",
                    Category               = AnnouncementCategory.LiveDataNudge,
                    CreatedByUserId        = adminUser?.Id ?? "system",
                    CreatedByName          = "System (Auto)",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "CampaignManager,FieldWorker",
                    IsPinned               = false,
                    RequiresAcknowledgement = false,
                    IsActive               = true,
                    CreatedAt              = now.AddHours(-1)
                },

                // 7. CAMPAIGN ANNOUNCEMENT — past, expired
                new Announcement
                {
                    Title                  = "Gandhi Nagar Padyatra — Route & Timing",
                    Body                   = "The padyatra through Gandhi Nagar (Ward 1) lanes has been confirmed.\n\n📍 Start: Gandhi Nagar Chowk\n🕙 Time: 7:00 AM\n🗺️ Route: Gandhi Nagar → Subhash Lane → Back to Chowk\n\nAll volunteers for Ward 1 must report at 6:45 AM. Wear campaign colours.",
                    Category               = AnnouncementCategory.CampaignAnnouncement,
                    CreatedByUserId        = managerUser?.Id ?? "system",
                    CreatedByName          = managerUser?.FullName ?? "Campaign Manager",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "FieldWorker,BoothAgent",
                    IsPinned               = false,
                    RequiresAcknowledgement = false,
                    IsActive               = true,
                    ExpiresAt              = now.AddDays(-18),   // already expired
                    CreatedAt              = now.AddDays(-22)
                },

                // 8. EC COMPLIANCE — model code reminder, active
                new Announcement
                {
                    Title                  = "Model Code of Conduct — Restricted Activity Window Begins Tonight",
                    Body                   = "As per the latest EC directive, the silent period begins tonight at midnight (00:00 hrs). The following activities are PROHIBITED during this window:\n\n❌ Public rallies or processions\n❌ Loudspeaker use in residential areas\n❌ Distribution of any material\n❌ Canvassing within 200m of a polling station\n\nViolations will be reported directly to the Returning Officer. All team members must comply strictly.",
                    Category               = AnnouncementCategory.ECComplianceNotice,
                    CreatedByUserId        = adminUser?.Id ?? "system",
                    CreatedByName          = adminUser?.FullName ?? "System Administrator",
                    ConstituencyId         = constituency.Id,
                    TargetRoles            = "All",
                    IsPinned               = false,
                    RequiresAcknowledgement = true,
                    IsActive               = true,
                    CreatedAt              = now.AddDays(-4)
                }
            );
            await db.SaveChangesAsync();
        }

        // ── Competitor Intelligence ──────────────────────────────────────────────
        if (!db.CompetitorActivities.Any())
        {
            var constituency = db.Constituencies.First();
            var now = DateTime.UtcNow;
            db.CompetitorActivities.AddRange(
                new CompetitorActivity { CompetitorName = "Suresh Waghmare",   PartyName = "Vikas Party",      ActivityTitle = "Grand Rally at Nehru Vihar Ground",              ActivityType = CompetitorActivityType.Rally,        Location = "Nehru Vihar Ground, Ward 2",         Ward = "2", BoothNumber = 2,  ActivityDate = now.AddDays(-18), EstimatedCrowd = 800,  ThreatLevel = CompetitorThreatLevel.High,     Notes = "Well-organised rally with senior party leaders present. Crowd mostly from Wards 2 & 3.",                               ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-18) },
                new CompetitorActivity { CompetitorName = "Suresh Waghmare",   PartyName = "Vikas Party",      ActivityTitle = "Door-to-Door Campaign — Patel Wadi",             ActivityType = CompetitorActivityType.DoorToDoor,   Location = "Patel Wadi, Ward 7",                 Ward = "7", BoothNumber = 7,  ActivityDate = now.AddDays(-15), EstimatedCrowd = null, ThreatLevel = CompetitorThreatLevel.Medium,   Notes = "Team of ~25 volunteers covered approx 150 households. Focussing on anti-incumbency sentiment.",                        ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-15) },
                new CompetitorActivity { CompetitorName = "Meera Khandare",    PartyName = "Jan Shakti Front", ActivityTitle = "Road Show — Shivaji Park to Tilak Nagar",        ActivityType = CompetitorActivityType.RoadShow,     Location = "Shivaji Park to Tilak Nagar, Ward 3-5", Ward = "3", BoothNumber = 3, ActivityDate = now.AddDays(-13), EstimatedCrowd = 350,  ThreatLevel = CompetitorThreatLevel.Medium,   Notes = "Procession of ~15 vehicles with loudspeakers. Covered three wards. Moderate crowd participation.",                    ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-13) },
                new CompetitorActivity { CompetitorName = "Suresh Waghmare",   PartyName = "Vikas Party",      ActivityTitle = "Press Conference on Water Supply Issue",         ActivityType = CompetitorActivityType.MediaCoverage, Location = "Press Club, Pune",                  Ward = null, BoothNumber = null, ActivityDate = now.AddDays(-11), EstimatedCrowd = null, ThreatLevel = CompetitorThreatLevel.High,   Notes = "Competitor gave media byte on water crisis in Ward 3 — getting local news coverage. Could sway floating voters.",     ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-11) },
                new CompetitorActivity { CompetitorName = "Meera Khandare",    PartyName = "Jan Shakti Front", ActivityTitle = "Women Voters Meeting — Laxmi Nagar",             ActivityType = CompetitorActivityType.SmallMeeting, Location = "Laxmi Nagar Community Hall, Ward 8",Ward = "8", BoothNumber = 8,  ActivityDate = now.AddDays(-9),  EstimatedCrowd = 90,   ThreatLevel = CompetitorThreatLevel.Medium,   Notes = "Targeted women voters with promises around safety and housing schemes. Attendance was decent.",                        ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-9)  },
                new CompetitorActivity { CompetitorName = "Deepak Sarpotdar",  PartyName = "Rashtra Sena",     ActivityTitle = "Youth Rally — Gandhi Nagar",                     ActivityType = CompetitorActivityType.Rally,        Location = "Gandhi Nagar Ground, Ward 1",        Ward = "1", BoothNumber = 1,  ActivityDate = now.AddDays(-8),  EstimatedCrowd = 220,  ThreatLevel = CompetitorThreatLevel.Low,      Notes = "Third candidate, smaller following. Rally targeted college students. Limited political influence expected.",            ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-8)  },
                new CompetitorActivity { CompetitorName = "Suresh Waghmare",   PartyName = "Vikas Party",      ActivityTitle = "Social Media Campaign — #WardsBroken",          ActivityType = CompetitorActivityType.SocialMedia,  Location = "Online / Twitter & Facebook",        Ward = null, BoothNumber = null, ActivityDate = now.AddDays(-7), EstimatedCrowd = null, ThreatLevel = CompetitorThreatLevel.High,   Notes = "Viral posts blaming incumbent for road & drainage neglect. Hashtag trending locally. Counter-messaging required.",    ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-7)  },
                new CompetitorActivity { CompetitorName = "Suresh Waghmare",   PartyName = "Vikas Party",      ActivityTitle = "Ambedkar Colony Large Meeting",                  ActivityType = CompetitorActivityType.SmallMeeting, Location = "Ambedkar Colony Chowk, Ward 4",     Ward = "4", BoothNumber = 4,  ActivityDate = now.AddDays(-5),  EstimatedCrowd = 310,  ThreatLevel = CompetitorThreatLevel.Critical, Notes = "Surprise high-attendance meeting in a key ward. Senior leader in attendance. URGENT — assign extra volunteers to Ward 4.", ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-5) },
                new CompetitorActivity { CompetitorName = "Meera Khandare",    PartyName = "Jan Shakti Front", ActivityTitle = "Announcement — Free Cycle Scheme Promise",       ActivityType = CompetitorActivityType.Announcement, Location = "Subhash Chowk, Ward 6",             Ward = "6", BoothNumber = 6,  ActivityDate = now.AddDays(-4),  EstimatedCrowd = null, ThreatLevel = CompetitorThreatLevel.Medium,   Notes = "Announced a free cycle scheme for girls. Could attract youth and women voters in Wards 5-7.",                         ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-4)  },
                new CompetitorActivity { CompetitorName = "Deepak Sarpotdar",  PartyName = "Rashtra Sena",     ActivityTitle = "Small Meeting — Booth 5 Voters",                 ActivityType = CompetitorActivityType.Other,        Location = "Tilak Nagar, Ward 5",               Ward = "5", BoothNumber = 5,  ActivityDate = now.AddDays(-3),  EstimatedCrowd = 55,   ThreatLevel = CompetitorThreatLevel.Low,      Notes = "Informal gathering with local contractor network. Low threat but monitor if aligns with Ward 7 contractor community.", ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-3)  },
                new CompetitorActivity { CompetitorName = "Suresh Waghmare",   PartyName = "Vikas Party",      ActivityTitle = "Mega Road Show Announced for This Weekend",      ActivityType = CompetitorActivityType.Announcement, Location = "Entire Constituency",               Ward = null, BoothNumber = null, ActivityDate = now.AddDays(-1), EstimatedCrowd = null, ThreatLevel = CompetitorThreatLevel.Critical, Notes = "Full-constituency road show planned. Senior national leader likely to attend. Must intensify our ward coverage immediately.", ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-1) },
                new CompetitorActivity { CompetitorName = "Meera Khandare",    PartyName = "Jan Shakti Front", ActivityTitle = "Social Media Post — Drainage Problem Video",     ActivityType = CompetitorActivityType.SocialMedia,  Location = "Online / Instagram & YouTube",      Ward = "7", BoothNumber = 7,  ActivityDate = now,              EstimatedCrowd = null, ThreatLevel = CompetitorThreatLevel.Medium,   Notes = "Video of flooded lane in Ward 7 shared with 2k+ views. Need rapid response content from our side.",                   ConstituencyId = constituency.Id, CreatedAt = now              }
            );
            await db.SaveChangesAsync();
        }

        // ── Influencer / Community Leader Network ─────────────────────────────
        if (!db.Influencers.Any())
        {
            var constituency = db.Constituencies.First();
            var now = DateTime.UtcNow;
            db.Influencers.AddRange(
                new Influencer { Name = "Pandit Ramesh Shastri",   MobileNumber = "9823400001", Category = "Religious",  Community = "Hindu – Brahmin",        EstimatedFollowers = 600,  Ward = "1", BoothNumber = 1, Alignment = InfluencerAlignment.Favour,   LastMetAt = now.AddDays(-10), LastMeetingOutcome = "Agreed to address voters at our rally. Strong supporter.",                      Notes = "Highly respected temple priest in Gandhi Nagar. Commands loyalty of approx 600 families.",          ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-30) },
                new Influencer { Name = "Maulana Iqbal Siddiqui",  MobileNumber = "9823400002", Category = "Religious",  Community = "Muslim",                 EstimatedFollowers = 450,  Ward = "2", BoothNumber = 2, Alignment = InfluencerAlignment.Floating, LastMetAt = now.AddDays(-7),  LastMeetingOutcome = "Open to supporting but wants sewage issue resolved. Follow-up scheduled.",         Notes = "Imam of main mosque in Nehru Vihar. Influential with ~450 voters. Needs grievance attention.",       ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-28) },
                new Influencer { Name = "Sarpanch Vitthal Kamble", MobileNumber = "9823400003", Category = "Caste",      Community = "Dalit – Mahar",          EstimatedFollowers = 700,  Ward = "4", BoothNumber = 4, Alignment = InfluencerAlignment.Favour,   LastMetAt = now.AddDays(-5),  LastMeetingOutcome = "Confirmed support. Will mobilise booth 4 voters on election day.",                   Notes = "Former sarpanch. Very influential in Ambedkar Colony. Controls ~700 Dalit votes in Ward 4.",         ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-25) },
                new Influencer { Name = "Bhai Gurpreet Singh",     MobileNumber = "9823400004", Category = "Religious",  Community = "Sikh",                   EstimatedFollowers = 120,  Ward = "3", BoothNumber = 3, Alignment = InfluencerAlignment.Neutral,  LastMetAt = now.AddDays(-12), LastMeetingOutcome = "Polite but non-committal. Wants to know candidate's stance on farmers.",            Notes = "Head of the Gurudwara in Ward 3. Small but cohesive community of ~120 voters.",                      ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-22) },
                new Influencer { Name = "Asha Tai Bharatkar",      MobileNumber = "9823400005", Category = "Women",      Community = "Women's SHG Network",    EstimatedFollowers = 950,  Ward = "5", BoothNumber = 5, Alignment = InfluencerAlignment.Favour,   LastMetAt = now.AddDays(-3),  LastMeetingOutcome = "Enthusiastic supporter. Has agreed to host voter awareness meetings in her network.", Notes = "Leader of 45 self-help groups across Wards 5 & 6. Extremely influential with women voters.",        ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-20) },
                new Influencer { Name = "Coach Dilip Shirke",      MobileNumber = "9823400006", Category = "Youth",      Community = "Sports / Youth",         EstimatedFollowers = 380,  Ward = "6", BoothNumber = 6, Alignment = InfluencerAlignment.Floating, LastMetAt = now.AddDays(-8),  LastMeetingOutcome = "Interested but wants a sports complex promise in writing. Escalated to candidate.",  Notes = "Football & cricket coach running free coaching for youth. Popular in Wards 6 & 7.",                 ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-18) },
                new Influencer { Name = "Kisan Neta Bharat Jagtap",MobileNumber = "9823400007", Category = "Farmer",     Community = "OBC – Kunbi",            EstimatedFollowers = 520,  Ward = "7", BoothNumber = 7, Alignment = InfluencerAlignment.Against,  LastMetAt = now.AddDays(-14), LastMeetingOutcome = "Unhappy with lack of irrigation support. Leaning toward competitor. Urgent re-engagement needed.", Notes = "President of local farmers' union. Significant sway over OBC agri-community in Ward 7.",    ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-16) },
                new Influencer { Name = "Fr. Anthony Pereira",     MobileNumber = "9823400008", Category = "Religious",  Community = "Christian",              EstimatedFollowers = 200,  Ward = "8", BoothNumber = 8, Alignment = InfluencerAlignment.Neutral,  LastMetAt = now.AddDays(-6),  LastMeetingOutcome = "Appreciated candidate's visit. Wants school repair in Ward 8. No commitment yet.",  Notes = "Parish priest of the Catholic church in Laxmi Nagar. Trusted by ~200 families.",                    ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-15) },
                new Influencer { Name = "Trade Leader Mohan Gupte",MobileNumber = "9823400009", Category = "Trader",     Community = "Vaishya – Marwari",      EstimatedFollowers = 310,  Ward = "1", BoothNumber = 1, Alignment = InfluencerAlignment.Favour,   LastMetAt = now.AddDays(-9),  LastMeetingOutcome = "Pledged support. Has agreed to display banners at all shops in the market.",         Notes = "President of the Gandhi Nagar traders' association. Influences ~310 business-community voters.",    ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-14) },
                new Influencer { Name = "Retired Colonel R.K. Nair",MobileNumber = "9823400010", Category = "Veterans",  Community = "Ex-Servicemen",          EstimatedFollowers = 240,  Ward = "3", BoothNumber = 3, Alignment = InfluencerAlignment.Floating, LastMetAt = now.AddDays(-11), LastMeetingOutcome = "Appreciated focus on law and order but wants clarification on defence pension policy.", Notes = "Ex-servicemen's association head. Respected across all wards. Bloc of ~240 votes.",                 ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-12) },
                new Influencer { Name = "Sunita Tambe",            MobileNumber = "9823400011", Category = "Caste",      Community = "Maratha",                EstimatedFollowers = 830,  Ward = "2", BoothNumber = 2, Alignment = InfluencerAlignment.Unknown,  LastMetAt = null,             LastMeetingOutcome = null,                                                                                   Notes = "Emerging Maratha community leader in Ward 2. Not yet met — outreach priority.",                      ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-10) },
                new Influencer { Name = "Prof. Nalini Godse",      MobileNumber = "9823400012", Category = "Education",  Community = "Academic / Teachers",    EstimatedFollowers = 160,  Ward = "5", BoothNumber = 5, Alignment = InfluencerAlignment.Favour,   LastMetAt = now.AddDays(-4),  LastMeetingOutcome = "Will encourage students and parents to vote. Offered to speak at our event.",         Notes = "Principal of the local college. Respected by educated middle-class voters in Ward 5.",               ConstituencyId = constituency.Id, IsActive = true, CreatedAt = now.AddDays(-9)  }
            );
            await db.SaveChangesAsync();
        }

        // ── Phone Banking & Call Logs ─────────────────────────────────────────
        if (!db.PhoneCallLogs.Any())
        {
            var constituency = db.Constituencies.First();
            var workerUser   = await userManager.FindByEmailAsync("worker@election.com");
            var managerUser  = await userManager.FindByEmailAsync("manager@election.com");
            var now          = DateTime.UtcNow;

            var voters = await db.Voters
                .Where(v => v.ConstituencyId == constituency.Id && v.MobileNumber != null)
                .Take(20).ToListAsync();

            if (!voters.Any())
            {
                // Assign mobile numbers to first 20 voters so call logs have valid links
                var allVoters = await db.Voters
                    .Where(v => v.ConstituencyId == constituency.Id)
                    .Take(20).ToListAsync();
                for (int i = 0; i < allVoters.Count; i++)
                    allVoters[i].MobileNumber = $"98765{(i + 1):D5}";
                await db.SaveChangesAsync();
                voters = allVoters;
            }

            var rnd = new Random(99);
            var outcomes  = new[] { CallOutcome.Talked, CallOutcome.NoAnswer, CallOutcome.CallBack, CallOutcome.Talked, CallOutcome.Talked, CallOutcome.Refused, CallOutcome.NoAnswer, CallOutcome.Talked };
            var sentiments = new VoterSentiment?[] { VoterSentiment.Favour, null, null, VoterSentiment.Floating, VoterSentiment.Favour, VoterSentiment.Against, null, VoterSentiment.Neutral };
            var notePool  = new[]
            {
                "Voter confirmed support. Will vote and bring family members.",
                "No answer on first attempt. Will retry in the evening.",
                "Requested callback after 6 PM — said they are at work.",
                "Interested but has concerns about road repair. Noted for follow-up.",
                "Strongly in favour. Shared with neighbours.",
                "Not interested in speaking. Disconnected.",
                "Phone was busy. Will try again tomorrow.",
                "Discussed water supply issue. Agreed to vote if candidate addresses it.",
                "Elderly voter, needed more time to explain. Very warm response.",
                "Asked about youth employment scheme. Shared details — seemed convinced.",
                "Said they have already decided to vote for our candidate.",
                "Concerned about pension delays. Raised grievance — logged separately.",
                "Very enthusiastic. Volunteered to help on election day.",
                "Mentioned competitor's rally. Engaged with counter-messaging.",
                "Floating voter — needs one more call. Marked for priority follow-up.",
            };

            var logs = new List<PhoneCallLog>();
            for (int i = 0; i < voters.Count && i < 30; i++)
            {
                var voter   = voters[i % voters.Count];
                var caller  = i % 3 == 0 ? managerUser : workerUser;
                var outcome = outcomes[i % outcomes.Length];
                var sentiment = sentiments[i % sentiments.Length];
                var daysAgo   = i / 5;          // spread over ~6 days
                var hoursAgo  = rnd.Next(0, 10);

                logs.Add(new PhoneCallLog
                {
                    VoterId            = voter.Id,
                    CalledByUserId     = caller?.Id ?? "system",
                    CalledByName       = caller?.FullName ?? "System",
                    CalledAt           = now.AddDays(-daysAgo).AddHours(-hoursAgo),
                    Outcome            = outcome,
                    DurationSeconds    = outcome == CallOutcome.Talked ? rnd.Next(60, 420)
                                       : outcome == CallOutcome.CallBack ? rnd.Next(15, 45) : 0,
                    Notes              = notePool[i % notePool.Length],
                    SentimentAfterCall = outcome == CallOutcome.Talked ? sentiment : null,
                    ConstituencyId     = constituency.Id
                });

                // Also update voter LastContactedAt and sentiment if Talked
                if (outcome == CallOutcome.Talked)
                {
                    voter.LastContactedAt = now.AddDays(-daysAgo).AddHours(-hoursAgo);
                    if (sentiment.HasValue)
                        voter.Sentiment = sentiment.Value;
                }
            }

            db.PhoneCallLogs.AddRange(logs);
            await db.SaveChangesAsync();
        }

        // ?? Audit Logs (seed representative history) ????????????????????????????????????
        if (!db.AuditLogs.Any())
        {
            var now = DateTime.UtcNow;
            var adminUser  = await userManager.FindByEmailAsync("admin@election.com");
            var managerUser = await userManager.FindByEmailAsync("manager@election.com");
            var workerUser  = await userManager.FindByEmailAsync("worker@election.com");

            var logs = new List<AuditLog>();
            void Add(AppUser? u, string action, string entity, string? entityId, string details, int daysAgo, int? cId = null) =>
                logs.Add(new AuditLog
                {
                    UserId       = u?.Id ?? "system",
                    UserName     = u?.FullName ?? "System",
                    Action       = action,
                    EntityType   = entity,
                    EntityId     = entityId,
                    Details      = details,
                    ConstituencyId = cId ?? u?.ConstituencyId,
                    IpAddress    = "127.0.0.1",
                    CreatedAt    = now.AddDays(-daysAgo).AddHours(-new Random(daysAgo).Next(0, 23))
                });

            Add(adminUser,   "Login",            "Session",    null, "Admin logged in",                                                   30);
            Add(adminUser,   "CreateUser",        "AppUser",    null, "Created user: Campaign Manager (manager@election.com)",             29);
            Add(adminUser,   "CreateUser",        "AppUser",    null, "Created user: Field Worker 1 (worker@election.com)",                29);
            Add(managerUser, "Login",             "Session",    null, "Manager logged in",                                                28);
            Add(workerUser,  "Login",             "Session",    null, "Field Worker logged in",                                           27);
            Add(managerUser, "PostAnnouncement",  "Announcement", null, "[Campaign] 'Ward 3 Padyatra Schedule' → FieldWorker,BoothAgent", 25);
            Add(workerUser,  "LogVisit",          "Voter",      "12", "Visit logged for Rajesh Kumar (MH010012): Visited, sentiment=Favour", 24);
            Add(workerUser,  "UpdateSentiment",   "Voter",      "5",  "Sentiment changed from Unknown to Favour for Sanjay Gupta (MH010005)", 23);
            Add(adminUser,   "Login",             "Session",    null, "Admin logged in",                                                   22);
            Add(managerUser, "LogVisit",          "Voter",      "18", "Visit logged for Priya Singh (MH010018): Visited, sentiment=Favour", 20);
            Add(workerUser,  "UpdateSentiment",   "Voter",      "22", "Sentiment changed from Neutral to Floating for Meena Joshi (MH020002)", 18);
            Add(managerUser, "PostAnnouncement",  "Announcement", null, "[CriticalAlert] 'EVM Test at Booth 3 — Stand By' → All",         17);
            Add(workerUser,  "Login",             "Session",    null, "Field Worker logged in",                                           16);
            Add(workerUser,  "LogVisit",          "Voter",      "34", "Visit logged for Amit Patel (MH020014): NotAtHome, sentiment=Unknown", 15);
            Add(managerUser, "Login",             "Session",    null, "Manager logged in",                                                14);
            Add(adminUser,   "EnableUser",        "AppUser",    null, "User 'Field Worker 1' enabled",                                    13);
            Add(managerUser, "PostAnnouncement",  "Announcement", null, "[DailyBriefing] 'Morning targets — Ward 5 & 6' → FieldWorker,BoothAgent", 12);
            Add(workerUser,  "UpdateSentiment",   "Voter",      "9",  "Sentiment changed from Against to Neutral for Kavitha Nair (MH010009)", 10);
            Add(workerUser,  "LogVisit",          "Voter",      "41", "Visit logged for Sunita Sharma (MH030001): Visited, sentiment=Favour", 9);
            Add(managerUser, "LogVisit",          "Voter",      "55", "Visit logged for Vijay Rao (MH030015): Refused, sentiment=Against", 8);
            Add(adminUser,   "Login",             "Session",    null, "Admin logged in",                                                    7);
            Add(adminUser,   "PostAnnouncement",  "Announcement", null, "[ECComplianceNotice] 'EC Expense Limit Reminder' → All",           6);
            Add(workerUser,  "UpdateSentiment",   "Voter",      "3",  "Sentiment changed from Floating to Favour for Dinesh Patil (MH010003)", 5);
            Add(managerUser, "Login",             "Session",    null, "Manager logged in",                                                    4);
            Add(workerUser,  "LogVisit",          "Voter",      "27", "Visit logged for Anita More (MH020007): Visited, sentiment=Favour",   3);
            Add(managerUser, "PostAnnouncement",  "Announcement", null, "[Motivation] 'We crossed 800 favour contacts!' → All",              2);
            Add(workerUser,  "Login",             "Session",    null, "Field Worker logged in",                                               1);
            Add(workerUser,  "UpdateSentiment",   "Voter",      "47", "Sentiment changed from Unknown to Favour for Rajesh Kumar (MH030007)", 0);

            db.AuditLogs.AddRange(logs);
            await db.SaveChangesAsync();
        }

        // ── Door-to-Door Visits ───────────────────────────────────────────────
        if (!db.DoorToDoorVisits.Any())
        {
            var constituency = db.Constituencies.First();
            var workerUser   = await userManager.FindByEmailAsync("worker@election.com");
            var managerUser  = await userManager.FindByEmailAsync("manager@election.com");
            var now          = DateTime.UtcNow;

            var voters = await db.Voters
                .Where(v => v.ConstituencyId == constituency.Id && !v.IsDeleted)
                .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
                .Take(80).ToListAsync();

            var rnd = new Random(77);
            var statuses   = new[] { VisitStatus.Visited, VisitStatus.Visited, VisitStatus.Visited, VisitStatus.NotAtHome, VisitStatus.Refused };
            var sentiments = new[] { VoterSentiment.Favour, VoterSentiment.Favour, VoterSentiment.Floating, VoterSentiment.Against, VoterSentiment.Neutral, VoterSentiment.Favour };
            var notePool   = new[]
            {
                "Voter is very enthusiastic. Will bring 4 family members to vote.",
                "Not at home. Neighbour confirmed they support our candidate.",
                "Had a long discussion on road issues. Promised to vote.",
                "Floating voter. Agreed to watch candidate's speech before deciding.",
                "Against — grievance about drainage still unresolved.",
                "Very warm response. Fixed a follow-up visit for next week.",
                "New voter, first election. Explained voting process.",
                "Elderly couple at home. Arranged transport for election day.",
                "Discussed water supply issue. Will vote if resolved.",
                "Strongly in favour. Already convinced 3 neighbours.",
            };

            var visits = new List<DoorToDoorVisit>();
            for (int i = 0; i < voters.Count; i++)
            {
                var voter     = voters[i];
                var caller    = i % 4 == 0 ? managerUser : workerUser;
                var status    = statuses[i % statuses.Length];
                var sentiment = sentiments[i % sentiments.Length];
                var daysAgo   = rnd.Next(0, 20);
                var hoursAgo  = rnd.Next(0, 8);
                var visitedAt = now.AddDays(-daysAgo).AddHours(-hoursAgo);

                visits.Add(new DoorToDoorVisit
                {
                    VoterId             = voter.Id,
                    WorkerUserId        = caller?.Id ?? "system",
                    WorkerName          = caller?.FullName ?? "Field Worker 1",
                    VisitedAt           = visitedAt,
                    Status              = status,
                    SentimentAfterVisit = status == VisitStatus.Visited ? sentiment : VoterSentiment.Unknown,
                    Notes               = notePool[i % notePool.Length],
                });

                if (status == VisitStatus.Visited)
                {
                    voter.LastContactedAt = visitedAt;
                    voter.Sentiment       = sentiment;
                }
            }

            db.DoorToDoorVisits.AddRange(visits);
            await db.SaveChangesAsync();
        }

        // ── Panna Pramukhs ────────────────────────────────────────────────────
        if (!db.PannaPramukhs.Any())
        {
            var constituency = db.Constituencies.First();
            var now          = DateTime.UtcNow;
            db.PannaPramukhs.AddRange(
                new PannaPramukh { Name = "Sunil Khedkar",    Phone = "9765400001", BoothNumber = 1, PannaNumber = "1A", TotalVotersAssigned = 10, VotersContacted = 9,  IsActive = true, Notes = "Very active. Covers Gandhi Nagar lane 1.",       ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-25) },
                new PannaPramukh { Name = "Rekha Dange",      Phone = "9765400002", BoothNumber = 1, PannaNumber = "1B", TotalVotersAssigned = 10, VotersContacted = 7,  IsActive = true, Notes = "Covers Gandhi Nagar lane 2.",                     ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-25) },
                new PannaPramukh { Name = "Prakash Shinde",   Phone = "9765400003", BoothNumber = 1, PannaNumber = "1C", TotalVotersAssigned = 10, VotersContacted = 10, IsActive = true, Notes = "100% coverage. Excellent coordinator.",           ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-24) },
                new PannaPramukh { Name = "Anita Gaikwad",    Phone = "9765400004", BoothNumber = 2, PannaNumber = "2A", TotalVotersAssigned = 10, VotersContacted = 6,  IsActive = true, Notes = "Nehru Vihar, first panna.",                       ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-23) },
                new PannaPramukh { Name = "Vijay Mhatre",     Phone = "9765400005", BoothNumber = 2, PannaNumber = "2B", TotalVotersAssigned = 10, VotersContacted = 8,  IsActive = true, Notes = "Covers market area in Ward 2.",                   ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-22) },
                new PannaPramukh { Name = "Laxmi Rokade",     Phone = "9765400006", BoothNumber = 2, PannaNumber = "2C", TotalVotersAssigned = 10, VotersContacted = 4,  IsActive = true, Notes = "Needs follow-up. Several voters unreachable.",    ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-22) },
                new PannaPramukh { Name = "Ganesh Sutar",     Phone = "9765400007", BoothNumber = 3, PannaNumber = "3A", TotalVotersAssigned = 10, VotersContacted = 9,  IsActive = true, Notes = "Shivaji Park — very motivated volunteer.",        ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-20) },
                new PannaPramukh { Name = "Meera Kamble",     Phone = "9765400008", BoothNumber = 3, PannaNumber = "3B", TotalVotersAssigned = 10, VotersContacted = 5,  IsActive = true, Notes = "Working part-time. Needs support.",               ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-20) },
                new PannaPramukh { Name = "Raju Patil",       Phone = "9765400009", BoothNumber = 4, PannaNumber = "4A", TotalVotersAssigned = 10, VotersContacted = 10, IsActive = true, Notes = "Ambedkar Colony. All voters confirmed.",          ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-18) },
                new PannaPramukh { Name = "Sunanda Bhosale",  Phone = "9765400010", BoothNumber = 4, PannaNumber = "4B", TotalVotersAssigned = 10, VotersContacted = 7,  IsActive = true, Notes = "Ward 4. 3 floating voters need a follow-up.",     ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-18) },
                new PannaPramukh { Name = "Deepak Salve",     Phone = "9765400011", BoothNumber = 5, PannaNumber = "5A", TotalVotersAssigned = 10, VotersContacted = 8,  IsActive = true, Notes = "Tilak Nagar. Regular updates given to manager.",  ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-15) },
                new PannaPramukh { Name = "Priya Nikam",      Phone = "9765400012", BoothNumber = 5, PannaNumber = "5B", TotalVotersAssigned = 10, VotersContacted = 3,  IsActive = true, Notes = "New volunteer. Needs coaching.",                  ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-15) },
                new PannaPramukh { Name = "Sachin More",      Phone = "9765400013", BoothNumber = 6, PannaNumber = "6A", TotalVotersAssigned = 10, VotersContacted = 9,  IsActive = true, Notes = "Subhash Chowk. Excellent network in the area.",   ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-12) },
                new PannaPramukh { Name = "Kavitha Jadhav",   Phone = "9765400014", BoothNumber = 7, PannaNumber = "7A", TotalVotersAssigned = 10, VotersContacted = 6,  IsActive = true, Notes = "Patel Wadi. Some voters difficult to reach.",     ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-10) },
                new PannaPramukh { Name = "Narayan Deshpande", Phone = "9765400015", BoothNumber = 8, PannaNumber = "8A", TotalVotersAssigned = 10, VotersContacted = 8, IsActive = true, Notes = "Laxmi Nagar. Senior citizen votes confirmed.",    ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-8)  }
            );
            await db.SaveChangesAsync();
        }

        // ── Booth Shift Assignments ───────────────────────────────────────────
        if (!db.BoothShiftAssignments.Any())
        {
            var constituency  = db.Constituencies.First();
            var volunteers    = await db.Volunteers.Where(v => v.ConstituencyId == constituency.Id && v.IsActive).ToListAsync();
            var electionDay   = constituency.ElectionDate != default ? constituency.ElectionDate : DateTime.Today.AddDays(30);
            var morningStart  = electionDay.Date.AddHours(7);
            var afternoonStart= electionDay.Date.AddHours(12);
            var eveningStart  = electionDay.Date.AddHours(15);

            var shifts = new List<BoothShiftAssignment>();
            foreach (var vol in volunteers.Take(12))
            {
                var boothNums = (vol.AssignedBoothNumbers ?? "1")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : 1).First();

                shifts.Add(new BoothShiftAssignment
                {
                    VolunteerId    = vol.Id,
                    BoothNumber    = boothNums,
                    ShiftStart     = morningStart,
                    ShiftEnd       = afternoonStart,
                    Role           = vol.Task == VolunteerTask.BoothManagement ? ShiftRole.BoothAgent
                                   : vol.Task == VolunteerTask.Transport       ? ShiftRole.Transport
                                   : ShiftRole.Coordinator,
                    IsConfirmed    = true,
                    Notes          = $"Morning shift — {vol.AssignedArea ?? "General"}",
                    ConstituencyId = constituency.Id
                });
            }
            // Add afternoon shifts for 6 volunteers
            foreach (var vol in volunteers.Skip(2).Take(6))
            {
                var boothNums = (vol.AssignedBoothNumbers ?? "1")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : 1).First();

                shifts.Add(new BoothShiftAssignment
                {
                    VolunteerId    = vol.Id,
                    BoothNumber    = boothNums,
                    ShiftStart     = afternoonStart,
                    ShiftEnd       = eveningStart.AddHours(2),
                    Role           = ShiftRole.Observer,
                    IsConfirmed    = false,
                    Notes          = "Afternoon shift — pending confirmation",
                    ConstituencyId = constituency.Id
                });
            }

            db.BoothShiftAssignments.AddRange(shifts);
            await db.SaveChangesAsync();
        }

        // ── Rapid Response Items ──────────────────────────────────────────────
        if (!db.RapidResponseItems.Any())
        {
            var constituency = db.Constituencies.First();
            var managerUser  = await userManager.FindByEmailAsync("manager@election.com");
            var adminUser    = await userManager.FindByEmailAsync("admin@election.com");
            var now          = DateTime.UtcNow;

            db.RapidResponseItems.AddRange(
                new RapidResponseItem
                {
                    Title          = "Viral WhatsApp Message: False Claim About Candidate",
                    Description    = "A fabricated WhatsApp forward is circulating claiming the candidate has been arrested. The message includes a doctored news screenshot. It is spreading rapidly in Ward 3 & Ward 5 groups.",
                    Source         = "WhatsApp",
                    AffectedWards  = "3,5",
                    ThreatLevel    = RapidResponseThreat.Critical,
                    Status         = RapidResponseStatus.ResponseDrafted,
                    ResponseText   = "Official clarification issued: The candidate is actively campaigning and this message is completely false. Screenshot of the fake post has been reported to WhatsApp. Counter-message sent to all volunteer group admins.",
                    AssignedToName = managerUser?.FullName ?? "Campaign Manager",
                    AssignedToUserId = managerUser?.Id,
                    LoggedByUserId = managerUser?.Id,
                    ConstituencyId = constituency.Id,
                    DetectedAt     = now.AddHours(-3),
                    CreatedAt      = now.AddHours(-3)
                },
                new RapidResponseItem
                {
                    Title          = "Competitor Rally Disrupted Our Booth Agent in Ward 7",
                    Description    = "Reports from Ward 7 indicate that competitor supporters surrounded and intimidated our booth agent during their road show. The agent has reported this to us. May affect volunteer morale.",
                    Source         = "Local Media",
                    AffectedWards  = "7",
                    ThreatLevel    = RapidResponseThreat.High,
                    Status         = RapidResponseStatus.Deployed,
                    ResponseText   = "Incident reported to Election Commission. Additional security volunteer deployed to Ward 7. Booth agent reassigned with a buddy system.",
                    AssignedToName = managerUser?.FullName ?? "Campaign Manager",
                    AssignedToUserId = managerUser?.Id,
                    LoggedByUserId = managerUser?.Id,
                    ConstituencyId = constituency.Id,
                    DetectedAt     = now.AddDays(-2),
                    CreatedAt      = now.AddDays(-2)
                },
                new RapidResponseItem
                {
                    Title          = "Local Newspaper Negative Story on Water Supply Delay",
                    Description    = "A local Marathi newspaper ran a front-page story attributing the Ward 3 water supply failure to our candidate's inaction. The story is factually incorrect — work was sanctioned 2 months ago.",
                    Source         = "Local Media",
                    AffectedWards  = "3",
                    ThreatLevel    = RapidResponseThreat.High,
                    Status         = RapidResponseStatus.Resolved,
                    ResponseText   = "Press release issued with work order details and timeline. Campaign team visited Ward 3 and distributed the official response. Follow-up story published on day 3 with correction.",
                    AssignedToName = adminUser?.FullName ?? "System Administrator",
                    AssignedToUserId = adminUser?.Id,
                    LoggedByUserId = adminUser?.Id,
                    ConstituencyId = constituency.Id,
                    DetectedAt     = now.AddDays(-8),
                    ResolvedAt     = now.AddDays(-5),
                    CreatedAt      = now.AddDays(-8)
                },
                new RapidResponseItem
                {
                    Title          = "Rumour: Candidate Promised Land to Builder in Ward 4",
                    Description    = "Rumour spreading door-to-door in Ward 4 (Ambedkar Colony) that the candidate has secretly given land clearance to a builder, displacing local residents. No evidence exists.",
                    Source         = "Competitor",
                    AffectedWards  = "4",
                    ThreatLevel    = RapidResponseThreat.Medium,
                    Status         = RapidResponseStatus.Detected,
                    ResponseText   = null,
                    LoggedByUserId = managerUser?.Id,
                    ConstituencyId = constituency.Id,
                    DetectedAt     = now.AddHours(-1),
                    CreatedAt      = now.AddHours(-1)
                },
                new RapidResponseItem
                {
                    Title          = "Social Media: #VoteAgainstCorruption Trend Targeting Our Candidate",
                    Description    = "Twitter and Instagram hashtag #VoteAgainstCorruption is trending locally with posts tagging our candidate. Mostly driven by competitor party handles. About 500+ posts in last 6 hours.",
                    Source         = "Other",
                    AffectedWards  = null,
                    ThreatLevel    = RapidResponseThreat.Medium,
                    Status         = RapidResponseStatus.ResponseDrafted,
                    ResponseText   = "Counter-campaign launched on social media with #DevelopmentWithHonesty. Influencer network activated to share candidate's track record posts. Youth volunteers creating response reels.",
                    AssignedToName = managerUser?.FullName ?? "Campaign Manager",
                    AssignedToUserId = managerUser?.Id,
                    LoggedByUserId = managerUser?.Id,
                    ConstituencyId = constituency.Id,
                    DetectedAt     = now.AddDays(-1),
                    CreatedAt      = now.AddDays(-1)
                },
                new RapidResponseItem
                {
                    Title          = "Fake Voter Slip Distributed in Ward 6",
                    Description    = "Fake voter slips with wrong booth numbers have been distributed in Ward 6. Voters will be misdirected on election day. Multiple residents have reported this on phone.",
                    Source         = "WhatsApp",
                    AffectedWards  = "6",
                    ThreatLevel    = RapidResponseThreat.Critical,
                    Status         = RapidResponseStatus.Deployed,
                    ResponseText   = "Genuine voter slips redistributed door-to-door in Ward 6 by our volunteers. EC notified with physical evidence of fake slips. Booth agent instructed to guide any confused voters.",
                    AssignedToName = adminUser?.FullName ?? "System Administrator",
                    AssignedToUserId = adminUser?.Id,
                    LoggedByUserId = adminUser?.Id,
                    ConstituencyId = constituency.Id,
                    DetectedAt     = now.AddDays(-3),
                    CreatedAt      = now.AddDays(-3)
                }
            );
            await db.SaveChangesAsync();
        }

        // ── Field Reports ─────────────────────────────────────────────────────
        if (!db.FieldReports.Any())
        {
            var constituency = db.Constituencies.First();
            var workerUser   = await userManager.FindByEmailAsync("worker@election.com");
            var managerUser  = await userManager.FindByEmailAsync("manager@election.com");
            var now          = DateTime.UtcNow;

            db.FieldReports.AddRange(
                new FieldReport
                {
                    WorkerUserId      = workerUser?.Id ?? "system",
                    WorkerName        = workerUser?.FullName ?? "Field Worker 1",
                    ReportDate        = now.AddDays(-1).Date,
                    ContactsMade      = 28,
                    FavourContacts    = 18,
                    FloatingContacts  = 6,
                    AgainstContacts   = 4,
                    IssuesLogged      = 3,
                    Highlights        = "Covered all of Ward 1 Lane 3 today. Voters in this area are very enthusiastic. Found 3 households that had shifted — updated voter list accordingly.",
                    Challenges        = "Two addresses had locked houses. Will retry tomorrow morning.",
                    PlannedForTomorrow= "Complete Ward 1 Lane 4 and start Lane 5. Follow up on the 2 uncontacted Favour voters from today.",
                    Status            = FieldReportStatus.Reviewed,
                    ReviewerNotes     = "Good coverage. Keep up the pace. Prioritise floating voters in Lane 4.",
                    ReviewedByUserId  = managerUser?.Id,
                    ReviewedAt        = now.AddHours(-10),
                    ConstituencyId    = constituency.Id,
                    CreatedAt         = now.AddDays(-1)
                },
                new FieldReport
                {
                    WorkerUserId      = workerUser?.Id ?? "system",
                    WorkerName        = workerUser?.FullName ?? "Field Worker 1",
                    ReportDate        = now.AddDays(-2).Date,
                    ContactsMade      = 32,
                    FavourContacts    = 22,
                    FloatingContacts  = 5,
                    AgainstContacts   = 5,
                    IssuesLogged      = 2,
                    Highlights        = "Met the local temple trust committee members. All 7 confirmed support. Also met a group of auto drivers — 5 out of 7 are in favour.",
                    Challenges        = "One voter was very hostile and asked us not to visit again. Logged grievance about road near his house.",
                    PlannedForTomorrow= "Follow up with floating voters identified today. Visit the senior citizens welfare group in the morning.",
                    Status            = FieldReportStatus.Reviewed,
                    ReviewerNotes     = "Excellent day. The temple trust endorsement is valuable — publicise it on WhatsApp groups.",
                    ReviewedByUserId  = managerUser?.Id,
                    ReviewedAt        = now.AddDays(-1),
                    ConstituencyId    = constituency.Id,
                    CreatedAt         = now.AddDays(-2)
                },
                new FieldReport
                {
                    WorkerUserId      = workerUser?.Id ?? "system",
                    WorkerName        = workerUser?.FullName ?? "Field Worker 1",
                    ReportDate        = now.AddDays(-3).Date,
                    ContactsMade      = 20,
                    FavourContacts    = 12,
                    FloatingContacts  = 4,
                    AgainstContacts   = 4,
                    IssuesLogged      = 5,
                    Highlights        = "Distributed voter awareness material in Ward 2. Several voters were not aware of the correct booth location — corrected them.",
                    Challenges        = "Rain in the afternoon stopped fieldwork for 2 hours. Many voters in building societies are difficult to access without appointment.",
                    PlannedForTomorrow= "Focus on Ward 2 apartments. Coordinate with society chairman for group meeting.",
                    Status            = FieldReportStatus.Submitted,
                    ConstituencyId    = constituency.Id,
                    CreatedAt         = now.AddDays(-3)
                },
                new FieldReport
                {
                    WorkerUserId      = managerUser?.Id ?? "system",
                    WorkerName        = managerUser?.FullName ?? "Campaign Manager",
                    ReportDate        = now.AddDays(-1).Date,
                    ContactsMade      = 15,
                    FavourContacts    = 10,
                    FloatingContacts  = 3,
                    AgainstContacts   = 2,
                    IssuesLogged      = 1,
                    Highlights        = "Conducted phone banking session with 8 volunteers. Covered Ward 5 & 6 floating voters. Conversion rate was 60% — better than expected.",
                    Challenges        = "Some voter mobile numbers in the list are outdated. Need data clean-up.",
                    PlannedForTomorrow= "Phone banking for Ward 7 & 8. Update phone numbers in the system for contacted voters.",
                    Status            = FieldReportStatus.Flagged,
                    ReviewerNotes     = "Flagged: Please update the voter mobile numbers as mentioned. Also check if Exotel integration can automate this.",
                    ReviewedByUserId  = managerUser?.Id,
                    ReviewedAt        = now.AddHours(-5),
                    ConstituencyId    = constituency.Id,
                    CreatedAt         = now.AddDays(-1)
                },
                new FieldReport
                {
                    WorkerUserId      = workerUser?.Id ?? "system",
                    WorkerName        = workerUser?.FullName ?? "Field Worker 1",
                    ReportDate        = now.Date,
                    ContactsMade      = 12,
                    FavourContacts    = 9,
                    FloatingContacts  = 2,
                    AgainstContacts   = 1,
                    IssuesLogged      = 1,
                    Highlights        = "Morning session very productive. Met 3 panna pramukhs for coordination. Verified voter list for Panna 2A — all voters confirmed.",
                    Challenges        = "One voter requested transport for election day. Added to transport request list.",
                    PlannedForTomorrow= "Continue door-to-door in Ward 2. Meeting with Panna Pramukhs for Booth 2 in the evening.",
                    Status            = FieldReportStatus.Submitted,
                    ConstituencyId    = constituency.Id,
                    CreatedAt         = now
                }
            );
            await db.SaveChangesAsync();
        }

        // ── Message Templates & Broadcasts ────────────────────────────────────
        if (!db.MessageTemplates.Any())
        {
            var constituency = db.Constituencies.First();
            var managerUser  = await userManager.FindByEmailAsync("manager@election.com");

            db.MessageTemplates.AddRange(
                new MessageTemplate
                {
                    Title             = "Election Day Voting Reminder",
                    Body              = "🗳️ Dear {VoterName}, today is Election Day! Your vote matters.\n\n📍 Your Booth: Booth {BoothNumber} — {BoothName}\n🕐 Polling Hours: 7 AM – 6 PM\n\nPlease carry your Voter ID card. #YourVoteYourFuture",
                    Language          = "English",
                    Category          = MessageCategory.ElectionReminder,
                    ConstituencyId    = constituency.Id,
                    CreatedByUserId   = managerUser?.Id ?? "system",
                    CreatedAt         = DateTime.UtcNow.AddDays(-10)
                },
                new MessageTemplate
                {
                    Title             = "मतदान दिवस स्मरणपत्र (Marathi)",
                    Body              = "🗳️ प्रिय {VoterName}, आज मतदान दिवस आहे!\n\n📍 आपले मतदान केंद्र: बूथ {BoothNumber} — {BoothName}\n🕐 मतदानाची वेळ: सकाळी ७ ते संध्याकाळी ६\n\nमतदार ओळखपत्र सोबत आणा. तुमचे मत, तुमचे भवितव्य! 🙏",
                    Language          = "Marathi",
                    Category          = MessageCategory.ElectionReminder,
                    ConstituencyId    = constituency.Id,
                    CreatedByUserId   = managerUser?.Id ?? "system",
                    CreatedAt         = DateTime.UtcNow.AddDays(-10)
                },
                new MessageTemplate
                {
                    Title             = "Rally Event Invitation",
                    Body              = "🎉 You are invited to the Grand Public Rally!\n\n📍 Venue: Patel Wadi Sports Ground, Ward 7\n📅 Date: This Saturday\n🕓 Time: 4:00 PM\n\nCome and hear the vision for our constituency's future. Bring your family and friends! Free entry.",
                    Language          = "English",
                    Category          = MessageCategory.EventInvite,
                    ConstituencyId    = constituency.Id,
                    CreatedByUserId   = managerUser?.Id ?? "system",
                    CreatedAt         = DateTime.UtcNow.AddDays(-8)
                },
                new MessageTemplate
                {
                    Title             = "Voter Outreach — First-Time Voters",
                    Body              = "👋 Hello {VoterName}! Welcome to your first election.\n\nVoting is easy:\n1️⃣ Carry your Voter ID (EPIC card)\n2️⃣ Go to Booth {BoothNumber}\n3️⃣ Show ID, get ballot, vote in the EVM\n\nNeed any help? Call us: {CampaignPhone}. We are here for you! 🗳️",
                    Language          = "English",
                    Category          = MessageCategory.VoterOutreach,
                    ConstituencyId    = constituency.Id,
                    CreatedByUserId   = managerUser?.Id ?? "system",
                    CreatedAt         = DateTime.UtcNow.AddDays(-7)
                },
                new MessageTemplate
                {
                    Title             = "Thank You Message — Post Election",
                    Body              = "🙏 Dear {VoterName},\n\nThank you for exercising your democratic right and voting today.\n\nYour support and trust mean everything to us. Together, we will build a better {ConstituencyName}.\n\nWith gratitude,\nDemo Candidate & Campaign Team 🇮🇳",
                    Language          = "English",
                    Category          = MessageCategory.ThankYou,
                    ConstituencyId    = constituency.Id,
                    CreatedByUserId   = managerUser?.Id ?? "system",
                    CreatedAt         = DateTime.UtcNow.AddDays(-5)
                },
                new MessageTemplate
                {
                    Title             = "EC Silence Period Notice",
                    Body              = "⚠️ Important Notice: The Election Commission's silent period begins tonight at midnight.\n\nNo campaign activities, rallies, or canvassing permitted for the next 48 hours as per the Model Code of Conduct.\n\nPlease comply with EC guidelines. Thank you.",
                    Language          = "English",
                    Category          = MessageCategory.Announcement,
                    ConstituencyId    = constituency.Id,
                    CreatedByUserId   = managerUser?.Id ?? "system",
                    CreatedAt         = DateTime.UtcNow.AddDays(-3)
                }
            );
            await db.SaveChangesAsync();

            // Seed broadcasts for the templates just created
            var templates = await db.MessageTemplates.Where(t => t.ConstituencyId == constituency.Id).ToListAsync();
            if (templates.Any())
            {
                db.MessageBroadcasts.AddRange(
                    new MessageBroadcast
                    {
                        TemplateId         = templates[0].Id,
                        TargetFilter       = "{\"sentiment\":\"Favour\"}",
                        TargetDescription  = "All Favour voters with mobile numbers",
                        TotalTargeted      = 85,
                        SentCount          = 82,
                        FailedCount        = 3,
                        Status             = BroadcastStatus.Sent,
                        ScheduledAt        = DateTime.UtcNow.AddDays(-1),
                        SentAt             = DateTime.UtcNow.AddDays(-1).AddMinutes(5),
                        ConstituencyId     = constituency.Id,
                        CreatedByUserId    = managerUser?.Id ?? "system",
                        CreatedByName      = managerUser?.FullName ?? "Campaign Manager",
                        CreatedAt          = DateTime.UtcNow.AddDays(-1)
                    },
                    new MessageBroadcast
                    {
                        TemplateId         = templates[2].Id,
                        TargetFilter       = "{\"wards\":[\"7\"]}",
                        TargetDescription  = "All voters in Ward 7 with mobile numbers",
                        TotalTargeted      = 110,
                        SentCount          = 107,
                        FailedCount        = 3,
                        Status             = BroadcastStatus.Sent,
                        ScheduledAt        = DateTime.UtcNow.AddDays(-6),
                        SentAt             = DateTime.UtcNow.AddDays(-6).AddMinutes(3),
                        ConstituencyId     = constituency.Id,
                        CreatedByUserId    = managerUser?.Id ?? "system",
                        CreatedByName      = managerUser?.FullName ?? "Campaign Manager",
                        CreatedAt          = DateTime.UtcNow.AddDays(-6)
                    },
                    new MessageBroadcast
                    {
                        TemplateId         = templates[3].Id,
                        TargetFilter       = "{\"sentiment\":\"Unknown\",\"ageGroup\":\"18-25\"}",
                        TargetDescription  = "First-time voters (18-25) with Unknown sentiment",
                        TotalTargeted      = 45,
                        SentCount          = 0,
                        FailedCount        = 0,
                        Status             = BroadcastStatus.Draft,
                        ConstituencyId     = constituency.Id,
                        CreatedByUserId    = managerUser?.Id ?? "system",
                        CreatedByName      = managerUser?.FullName ?? "Campaign Manager",
                        CreatedAt          = DateTime.UtcNow.AddHours(-2)
                    }
                );
                await db.SaveChangesAsync();
            }
        }

        // ── Voter Transport Requests ──────────────────────────────────────────
        if (!db.VoterTransportRequests.Any())
        {
            var constituency = db.Constituencies.First();
            var vehicles     = await db.TransportVehicles.Where(v => v.ConstituencyId == constituency.Id).ToListAsync();
            var voters       = await db.Voters
                .Where(v => v.ConstituencyId == constituency.Id && !v.IsDeleted)
                .OrderBy(v => v.BoothNumber).Take(20)
                .ToListAsync();
            var now          = DateTime.UtcNow;

            if (voters.Any() && vehicles.Any())
            {
                var requests = new List<VoterTransportRequest>();
                var statusList = new[] { TransportStatus.Voted, TransportStatus.PickedUp, TransportStatus.Assigned, TransportStatus.Pending, TransportStatus.Pending, TransportStatus.Cancelled };

                for (int i = 0; i < Math.Min(voters.Count, 15); i++)
                {
                    var voter   = voters[i];
                    var vehicle = vehicles[i % vehicles.Count];
                    var status  = statusList[i % statusList.Length];

                    requests.Add(new VoterTransportRequest
                    {
                        VoterId        = voter.Id,
                        VehicleId      = status == TransportStatus.Pending ? null : vehicle.Id,
                        PickupAddress  = voter.Address,
                        PickupNotes    = i % 3 == 0 ? "Senior citizen — needs assistance getting in vehicle."
                                       : i % 3 == 1 ? "Wheelchair user — requires accessible vehicle."
                                       : "Regular request.",
                        Status         = status,
                        RequestedAt    = now.AddDays(-1).AddHours(-i),
                        AssignedAt     = status != TransportStatus.Pending ? now.AddDays(-1).AddHours(-i + 1) : null,
                        PickedUpAt     = status == TransportStatus.PickedUp || status == TransportStatus.Voted
                                         ? now.AddHours(-i) : null,
                        ConstituencyId = constituency.Id
                    });
                }

                db.VoterTransportRequests.AddRange(requests);
                await db.SaveChangesAsync();
            }
        }

        // ── Budget Plans ──────────────────────────────────────────────────────
        if (!db.BudgetPlans.Any())
        {
            var constituency = db.Constituencies.First();
            db.BudgetPlans.AddRange(
                new BudgetPlan { Category = ExpenseCategory.Publicity,      PlannedAmount = 1_200_000m, Notes = "Flex banners, hoardings, pamphlets, social media ads.",      ConstituencyId = constituency.Id, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new BudgetPlan { Category = ExpenseCategory.Transport,       PlannedAmount = 500_000m,  Notes = "Vehicle hire for campaign team, election day voter transport.", ConstituencyId = constituency.Id, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new BudgetPlan { Category = ExpenseCategory.Food,            PlannedAmount = 400_000m,  Notes = "Refreshments at rallies and volunteer coordination meetings.",   ConstituencyId = constituency.Id, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new BudgetPlan { Category = ExpenseCategory.Communication,   PlannedAmount = 600_000m,  Notes = "SMS campaigns, WhatsApp broadcast, phone banking setup.",        ConstituencyId = constituency.Id, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new BudgetPlan { Category = ExpenseCategory.Printing,        PlannedAmount = 700_000m,  Notes = "Voter slips, manifesto copies, letter pads, receipts.",          ConstituencyId = constituency.Id, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new BudgetPlan { Category = ExpenseCategory.Miscellaneous,   PlannedAmount = 300_000m,  Notes = "Contingency, stationary, unforeseen campaign needs.",            ConstituencyId = constituency.Id, CreatedAt = DateTime.UtcNow.AddDays(-30) }
            );
            await db.SaveChangesAsync();
        }

        // ── Expenses ──────────────────────────────────────────────────────────
        if (!db.Expenses.Any())
        {
            var constituency = db.Constituencies.First();
            var adminUser    = await userManager.FindByEmailAsync("admin@election.com");
            var managerUser  = await userManager.FindByEmailAsync("manager@election.com");
            var now          = DateTime.UtcNow;

            db.Expenses.AddRange(
                new Expense { Description = "Flex banners for Ward 1–4 (200 pcs)",          Category = ExpenseCategory.Publicity,    Amount = 48_000m,  ExpenseDate = now.AddDays(-25).Date, PayeeName = "Shree Print Works",       VoucherNumber = "SPW-001", IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-25) },
                new Expense { Description = "Patel Wadi Rally — sound system hire",          Category = ExpenseCategory.Publicity,    Amount = 25_000m,  ExpenseDate = now.AddDays(-20).Date, PayeeName = "Ganesh Sound Services",   VoucherNumber = "GSS-045", IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-20) },
                new Expense { Description = "Vehicle hire — campaign team (5 days)",         Category = ExpenseCategory.Transport,    Amount = 35_000m,  ExpenseDate = now.AddDays(-18).Date, PayeeName = "Ravi Travels",            VoucherNumber = "RT-112",  IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-18) },
                new Expense { Description = "Nehru Vihar Rally — food for 500 volunteers",   Category = ExpenseCategory.Food,         Amount = 22_500m,  ExpenseDate = now.AddDays(-15).Date, PayeeName = "Laxmi Caterers",          VoucherNumber = "LC-088",  IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-15) },
                new Expense { Description = "Manifesto booklet printing (2000 copies)",      Category = ExpenseCategory.Printing,     Amount = 18_000m,  ExpenseDate = now.AddDays(-14).Date, PayeeName = "City Offset Press",       VoucherNumber = "COP-201", IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-14) },
                new Expense { Description = "WhatsApp broadcast campaign setup",              Category = ExpenseCategory.Communication,Amount = 15_000m,  ExpenseDate = now.AddDays(-12).Date, PayeeName = "Digital Edge Solutions",  VoucherNumber = "DES-077", IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-12) },
                new Expense { Description = "Hoardings — Ward 5, 6, 7 (15 hoardings)",      Category = ExpenseCategory.Publicity,    Amount = 60_000m,  ExpenseDate = now.AddDays(-10).Date, PayeeName = "Outdoor Media Co.",       VoucherNumber = "OMC-055", IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-10) },
                new Expense { Description = "Voter slip printing (5000 copies with QR)",     Category = ExpenseCategory.Printing,     Amount = 12_500m,  ExpenseDate = now.AddDays(-9).Date,  PayeeName = "City Offset Press",       VoucherNumber = "COP-209", IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-9) },
                new Expense { Description = "Field worker daily allowance (10 workers, 5 days)", Category = ExpenseCategory.Miscellaneous, Amount = 25_000m, ExpenseDate = now.AddDays(-8).Date, PayeeName = "Staff Payments",        VoucherNumber = "SP-003",  IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-8) },
                new Expense { Description = "Tilak Nagar Nukkad Natak — props & costumes",   Category = ExpenseCategory.Publicity,    Amount = 8_500m,   ExpenseDate = now.AddDays(-6).Date,  PayeeName = "Kala Kendra",             VoucherNumber = "KK-019",  IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-6) },
                new Expense { Description = "SMS blast — 10,000 messages (Exotel)",           Category = ExpenseCategory.Communication,Amount = 3_200m,   ExpenseDate = now.AddDays(-5).Date,  PayeeName = "Exotel Technologies",     VoucherNumber = "EXT-441", IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-5) },
                new Expense { Description = "Refreshments — panna pramukh training session",  Category = ExpenseCategory.Food,         Amount = 4_800m,   ExpenseDate = now.AddDays(-4).Date,  PayeeName = "Laxmi Caterers",          VoucherNumber = "LC-091",  IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-4) },
                new Expense { Description = "Campaign office stationery & supplies",          Category = ExpenseCategory.Miscellaneous,Amount = 6_200m,   ExpenseDate = now.AddDays(-3).Date,  PayeeName = "National Stationery",     VoucherNumber = "NS-102",  IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-3) },
                new Expense { Description = "Fuel reimbursement — volunteer vehicles",        Category = ExpenseCategory.Transport,    Amount = 12_000m,  ExpenseDate = now.AddDays(-2).Date,  PayeeName = "Volunteer Pool",          VoucherNumber = "VP-015",  IsECCompliant = true,  ApprovedByUserId = managerUser?.Id, ApprovedByName = managerUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-2) },
                new Expense { Description = "Final rally — PA system, generator, stage",      Category = ExpenseCategory.Publicity,    Amount = 95_000m,  ExpenseDate = now.AddDays(-1).Date,  PayeeName = "Event Masters Pvt Ltd",   VoucherNumber = "EM-333",  IsECCompliant = true,  ApprovedByUserId = adminUser?.Id, ApprovedByName = adminUser?.FullName, ConstituencyId = constituency.Id, CreatedAt = now.AddDays(-1) }
            );
            await db.SaveChangesAsync();
        }

        // ── Survey Candidates & Parties (Preference Analytics) ────────────────
        if (!await db.SurveyCandidates.AnyAsync() || !await db.SurveyParties.AnyAsync())
        {
            var constituency = await db.Constituencies.FirstAsync();

            if (!await db.SurveyParties.AnyAsync())
            {
                db.SurveyParties.AddRange(
                    new SurveyParty { Name = "Demo Party",        Symbol = "Lotus",   Notes = "Incumbent alliance partner",        ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyParty { Name = "Vikas Party",       Symbol = "Hand",    Notes = "Main opposition",                   ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyParty { Name = "Jan Shakti Front",  Symbol = "Cycle",   Notes = "Regional challenger",               ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyParty { Name = "Independent / Other", Symbol = "Other", Notes = "Independents and undecided party", ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.SurveyCandidates.AnyAsync())
            {
                db.SurveyCandidates.AddRange(
                    new SurveyCandidate { Name = "Demo Candidate",     PartyAffiliation = "Demo Party",       Notes = "Our candidate",              ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyCandidate { Name = "Suresh Waghmare",    PartyAffiliation = "Vikas Party",      Notes = "Main competitor",           ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyCandidate { Name = "Meera Khandare",     PartyAffiliation = "Jan Shakti Front", Notes = "Women-focused challenger",  ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyCandidate { Name = "Deepak Sarpotdar",   PartyAffiliation = "Rashtra Sena",     Notes = "Smaller third candidate",   ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new SurveyCandidate { Name = "Not Decided / None", PartyAffiliation = null,               Notes = "Indecisive or no party preference", ConstituencyId = constituency.Id, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) }
                );
                await db.SaveChangesAsync();
            }
        }

        // ── Reward config + coupons (Consent Analytics / survey rewards) ──────
        if (!await db.RewardConfigs.AnyAsync())
        {
            var constituency = await db.Constituencies.FirstAsync();
            var reward = new RewardConfig
            {
                ConstituencyId   = constituency.Id,
                Title            = "Voter Survey Thank-You Coupon",
                Description      = "₹50 partner coupon for completing the self-survey",
                PartnerBrand     = "Nirvachak Partner Store",
                CouponCodePrefix = "NIRV",
                ExpiryDate       = DateTime.UtcNow.AddMonths(3),
                IsActive         = true,
                CreatedAt        = DateTime.UtcNow.AddDays(-15)
            };
            db.RewardConfigs.Add(reward);
            await db.SaveChangesAsync();

            var coupons = new List<CouponPool>();
            for (int i = 1; i <= 80; i++)
            {
                coupons.Add(new CouponPool
                {
                    RewardConfigId = reward.Id,
                    CouponCode     = $"NIRV{i:D4}",
                    IsIssued       = false,
                    IsRedeemed     = false
                });
            }
            db.CouponPools.AddRange(coupons);
            await db.SaveChangesAsync();
        }

        // ── Voter Profiles, Consents, Survey Completions ─────────────────────
        // Powers: Preference Analytics + Voter Consent Analytics pages
        if (!await db.VoterProfiles.AnyAsync())
        {
            var constituency = await db.Constituencies.FirstAsync();
            var candidates = await db.SurveyCandidates
                .Where(c => c.ConstituencyId == constituency.Id && c.IsActive)
                .OrderBy(c => c.Id).ToListAsync();
            var parties = await db.SurveyParties
                .Where(p => p.ConstituencyId == constituency.Id && p.IsActive)
                .OrderBy(p => p.Id).ToListAsync();
            var voters = await db.Voters
                .Where(v => v.ConstituencyId == constituency.Id && !v.IsDeleted)
                .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
                .Take(70)
                .ToListAsync();

            if (voters.Any() && candidates.Any() && parties.Any())
            {
                var rnd = new Random(123);
                var ageBrackets = new[] { "18–25", "26–35", "36–50", "51–65", "65+" };
                var castes      = new[] { "General", "OBC", "SC", "ST", "NT" };
                var religions   = new[] { "Hindu", "Muslim", "Christian", "Sikh", "Buddhist", "Jain" };
                var education   = new[] { "Below 10th", "10th", "12th", "Graduate", "PG+" };
                var occupation  = new[] { "Farmer", "Service", "Business", "Student", "Homemaker", "Other" };
                var incomes     = new[] { "<10K", "10-25K", "25-50K", "50K+" };
                var languages   = new[] { "Marathi", "Hindi", "English" };
                var concernPool = new[]
                {
                    "Roads & Transport", "Water Supply", "Electricity", "Garbage & Sanitation",
                    "Employment", "Healthcare", "Education", "Women Safety", "Housing", "Pensions"
                };

                // Weighted candidate preference: Demo Candidate most popular
                int PickCandidateIndex(int i) => (i % 10) switch
                {
                    0 or 1 or 2 or 3 => 0, // Demo Candidate ~40%
                    4 or 5 or 6      => 1, // Suresh ~30%
                    7 or 8           => 2, // Meera ~20%
                    9               => i % 2 == 0 ? 3 : 4, // Deepak / Undecided
                    _               => 0
                };

                var profiles = new List<VoterProfile>();
                var consents = new List<VoterConsent>();
                var completions = new List<SurveyCompletion>();

                var reward = await db.RewardConfigs
                    .FirstOrDefaultAsync(r => r.ConstituencyId == constituency.Id && r.IsActive);
                var availableCoupons = reward == null
                    ? new List<CouponPool>()
                    : await db.CouponPools
                        .Where(c => c.RewardConfigId == reward.Id && !c.IsIssued)
                        .OrderBy(c => c.Id)
                        .Take(voters.Count)
                        .ToListAsync();

                int couponIdx = 0;
                for (int i = 0; i < voters.Count; i++)
                {
                    var voter = voters[i];
                    var candIdx = Math.Min(PickCandidateIndex(i), candidates.Count - 1);
                    var partyIdx = Math.Min(candIdx, parties.Count - 1);
                    // Align party loosely with candidate party name when possible
                    var cand = candidates[candIdx];
                    var matchedParty = parties.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(cand.PartyAffiliation) &&
                        p.Name.Contains(cand.PartyAffiliation.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
                        ?? parties[partyIdx];

                    var concerns = concernPool
                        .OrderBy(_ => rnd.Next())
                        .Take(rnd.Next(1, 4))
                        .ToList();

                    profiles.Add(new VoterProfile
                    {
                        VoterId                = voter.Id,
                        AgeBracket             = ageBrackets[i % ageBrackets.Length],
                        CasteCategory          = castes[i % castes.Length],
                        Religion               = religions[i % religions.Length],
                        Education              = education[i % education.Length],
                        Occupation             = occupation[i % occupation.Length],
                        MonthlyIncomeBracket   = incomes[i % incomes.Length],
                        PrimaryConcerns        = System.Text.Json.JsonSerializer.Serialize(concerns),
                        PreferredLanguage      = languages[i % languages.Length],
                        PhoneVerified          = i % 3 != 0,
                        PreferredCandidateId   = cand.Id,
                        PreferredPartyId       = matchedParty.Id,
                        CompletedAt            = DateTime.UtcNow.AddDays(-(i % 18)).AddHours(-(i % 12)),
                        IpAddress              = $"10.0.{(i / 50) + 1}.{i % 50 + 10}"
                    });

                    // Varied consent mix for analytics charts
                    consents.Add(new VoterConsent
                    {
                        VoterId                     = voter.Id,
                        AllowThirdPartyAdvertising  = i % 4 != 0, // ~75%
                        AllowCampaignOutreach       = i % 5 != 0, // ~80%
                        AllowWhatsAppMessages       = i % 3 != 0, // ~66%
                        AllowSchemeNotifications    = i % 2 == 0, // ~50%
                        AllowDataForAnalytics       = i % 6 != 0, // ~83%
                        ConsentGivenAt              = DateTime.UtcNow.AddDays(-(i % 18)).AddHours(-(i % 12)),
                        IpAddress                   = $"10.0.{(i / 50) + 1}.{i % 50 + 10}"
                    });

                    int? couponId = null;
                    if (couponIdx < availableCoupons.Count && i % 5 != 4) // ~80% get a coupon
                    {
                        var coupon = availableCoupons[couponIdx++];
                        coupon.IsIssued = true;
                        coupon.IssuedToVoterId = voter.Id;
                        coupon.IssuedAt = DateTime.UtcNow.AddDays(-(i % 18));
                        if (i % 7 == 0)
                        {
                            coupon.IsRedeemed = true;
                            coupon.RedeemedAt = DateTime.UtcNow.AddDays(-(i % 10));
                        }
                        couponId = coupon.Id;
                    }

                    completions.Add(new SurveyCompletion
                    {
                        VoterId         = voter.Id,
                        ConstituencyId  = constituency.Id,
                        CouponId        = couponId,
                        CompletedAt     = DateTime.UtcNow.AddDays(-(i % 18)).AddHours(-(i % 12)),
                        IpAddress       = $"10.0.{(i / 50) + 1}.{i % 50 + 10}"
                    });

                    if (string.IsNullOrEmpty(voter.MobileNumber))
                        voter.MobileNumber = $"98{(70000000 + i):D8}";
                }

                db.VoterProfiles.AddRange(profiles);
                db.VoterConsents.AddRange(consents);
                db.SurveyCompletions.AddRange(completions);
                await db.SaveChangesAsync();
            }
        }

        // ── Swing Voters demo pattern ─────────────────────────────────────────
        // Swing Voters page needs: current Floating/Against AND at least one past Favour visit.
        // Generic visit seed often ends voters on their last sentiment, so ensure a clear set.
        {
            var constituency = await db.Constituencies.FirstAsync();
            var workerUser = await userManager.FindByEmailAsync("worker@election.com")
                          ?? await userManager.FindByEmailAsync("manager@election.com");
            var workerId = workerUser?.Id ?? "system";
            var workerName = workerUser?.FullName ?? "Field Worker 1";

            var floatingOrAgainst = await db.Voters
                .Where(v => v.ConstituencyId == constituency.Id
                            && !v.IsDeleted
                            && (v.Sentiment == VoterSentiment.Floating || v.Sentiment == VoterSentiment.Against))
                .Select(v => v.Id)
                .ToListAsync();

            var favourVisitIds = await db.DoorToDoorVisits
                .Where(v => floatingOrAgainst.Contains(v.VoterId)
                            && v.SentimentAfterVisit == VoterSentiment.Favour)
                .Select(v => v.VoterId)
                .Distinct()
                .ToListAsync();

            // If fewer than 12 true swing voters, create/repair the pattern
            if (favourVisitIds.Count < 12)
            {
                var candidatesForSwing = await db.Voters
                    .Where(v => v.ConstituencyId == constituency.Id && !v.IsDeleted)
                    .OrderBy(v => v.BoothNumber).ThenBy(v => v.SerialNumber)
                    .Take(40)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                var newVisits = new List<DoorToDoorVisit>();
                int made = favourVisitIds.Count;

                for (int i = 0; i < candidatesForSwing.Count && made < 20; i++)
                {
                    var voter = candidatesForSwing[i];
                    if (favourVisitIds.Contains(voter.Id))
                        continue;

                    // Past Favour visit (makes them a "true swing" once current sentiment changes)
                    newVisits.Add(new DoorToDoorVisit
                    {
                        VoterId             = voter.Id,
                        WorkerUserId        = workerId,
                        WorkerName          = workerName,
                        VisitedAt           = now.AddDays(-(18 - (i % 10))).AddHours(-(i % 6)),
                        Status              = VisitStatus.Visited,
                        SentimentAfterVisit = VoterSentiment.Favour,
                        Notes               = "Earlier visit: voter was supportive / leaning Favour."
                    });

                    // Later visit that swung away
                    var nowSentiment = i % 3 == 0 ? VoterSentiment.Against : VoterSentiment.Floating;
                    newVisits.Add(new DoorToDoorVisit
                    {
                        VoterId             = voter.Id,
                        WorkerUserId        = workerId,
                        WorkerName          = workerName,
                        VisitedAt           = now.AddDays(-(3 - (i % 3))).AddHours(-(i % 5)),
                        Status              = VisitStatus.Visited,
                        SentimentAfterVisit = nowSentiment,
                        Notes               = nowSentiment == VoterSentiment.Against
                            ? "Follow-up: voter shifted Against after competitor outreach / unresolved grievance."
                            : "Follow-up: voter is now Floating — needs priority re-engagement."
                    });

                    voter.Sentiment = nowSentiment;
                    voter.LastContactedAt = now.AddDays(-(3 - (i % 3)));
                    if (string.IsNullOrEmpty(voter.MobileNumber))
                        voter.MobileNumber = $"99{(80000000 + i):D8}";

                    made++;
                }

                if (newVisits.Count > 0)
                {
                    db.DoorToDoorVisits.AddRange(newVisits);
                    await db.SaveChangesAsync();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[SEED] Added {newVisits.Count} swing-voter visit records ({made} swing voters).");
                    Console.ResetColor();
                }
            }
        }
    }
}

