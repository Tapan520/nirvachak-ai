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
    }
}
