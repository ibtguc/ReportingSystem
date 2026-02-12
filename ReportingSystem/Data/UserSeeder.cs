using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class UserSeeder
{
    private static DateTime _now;

    private static User U(string email, string name, SystemRole role = SystemRole.CommitteeUser, int rank = 0)
    {
        var u = new User
        {
            Email = email,
            Name = name,
            SystemRole = role,
            IsActive = true,
            CreatedAt = _now
        };
        if (rank > 0) u.ChairmanOfficeRank = rank;
        return u;
    }

    private static CommitteeMembership H(User u, Committee c)
        => new() { UserId = u.Id, CommitteeId = c.Id, Role = CommitteeRole.Head, EffectiveFrom = _now };

    private static CommitteeMembership M(User u, Committee c)
        => new() { UserId = u.Id, CommitteeId = c.Id, Role = CommitteeRole.Member, EffectiveFrom = _now };

    public static async Task SeedAdminUsersAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync()) return;
        _now = DateTime.UtcNow;

        // ═══════════════════════════════════════════
        //  USERS
        // ═══════════════════════════════════════════

        var admin           = U("admin@org.edu",              "System Administrator", SystemRole.SystemAdmin);
        var chairman        = U("am@org.edu",                 "AM",                  SystemRole.Chairman);

        // Chairman Office
        var ahmedMansour    = U("ahmed.mansour@org.edu",      "Ahmed Mansour",       SystemRole.ChairmanOffice, 1);
        var moustafaFouad   = U("moustafa.fouad@org.edu",     "Moustafa Fouad",      SystemRole.ChairmanOffice, 2);
        var marwaElSerafy   = U("marwa.elserafy@org.edu",     "Marwa El Serafy",     SystemRole.ChairmanOffice, 3);
        var samiaElAshiry   = U("samia.elashiry@org.edu",     "Samia El Ashiry",     SystemRole.ChairmanOffice, 4);

        // Top Level Committee heads
        var mohamedIbrahim  = U("mohamed.ibrahim@org.edu",    "Mohamed Ibrahim");
        var radwaSelim      = U("radwa.selim@org.edu",        "Radwa Selim");
        var ghadirNassar    = U("ghadir.nassar@org.edu",      "Ghadir Nassar");
        var engyGalal       = U("engy.galal@org.edu",         "Engy Galal");
        var karimSalme      = U("karim.salme@org.edu",        "Karim Salme");
        var sherineKhalil   = U("sherine.khalil@org.edu",     "Sherine Khalil");
        var sherineSalamony = U("sherine.salamony@org.edu",   "Sherine Salamony");

        // ── Named L2 heads ──
        var ohoudKhadr      = U("ohoud.khadr@org.edu",        "Ohoud Khadr");
        var yehiaRazzaz     = U("yehia.razzaz@org.edu",       "Yehia Razzaz");
        var amrBaibars      = U("amr.baibars@org.edu",        "Amr Baibars");
        var ibrahimKhalil   = U("ibrahim.khalil@org.edu",     "Gen. Ibrahim Khalil");
        var daliaElMainouny = U("dalia.elmainouny@org.edu",   "Dalia El Mainouny");
        var aymanRahmou     = U("ayman.rahmou@org.edu",       "Ayman Rahmou");
        var salmaIbrahim    = U("salma.ibrahim@org.edu",      "Salma Ibrahim");

        // ── Generated L2 heads ──
        var hananMostafa      = U("hanan.mostafa@org.edu",      "Hanan Mostafa");        // Curriculum
        var tarekAbdelFattah  = U("tarek.abdelfattah@org.edu",  "Tarek Abdel Fattah");   // Probation & Mentoring
        var nohaElSayed       = U("noha.elsayed@org.edu",       "Noha El Sayed");        // Teaching & Evaluation
        var laylaHassan       = U("layla.hassan@org.edu",       "Layla Hassan");          // Theater
        var ramyShawky        = U("ramy.shawky@org.edu",        "Ramy Shawky");           // Admission Services
        var hossamBadawy      = U("hossam.badawy@org.edu",      "Hossam Badawy");        // Security
        var monaFarid         = U("mona.farid@org.edu",         "Mona Farid");            // Agriculture

        // ── Generated L2 members ──
        // Curriculum
        var amiraSoliman      = U("amira.soliman@org.edu",      "Amira Soliman");
        var bassemYoussef     = U("bassem.youssef@org.edu",     "Bassem Youssef");
        // Probation & Mentoring
        var fatmaElZahraa     = U("fatma.elzahraa@org.edu",     "Fatma El Zahraa");
        var omarHashem        = U("omar.hashem@org.edu",        "Omar Hashem");
        // Teaching & Evaluation
        var yasminAbdelRahman = U("yasmin.abdelrahman@org.edu", "Yasmin Abdel Rahman");
        var khaledMostafa     = U("khaled.mostafa@org.edu",     "Khaled Mostafa");
        // Music
        var nadiaFawzy        = U("nadia.fawzy@org.edu",        "Nadia Fawzy");
        var waelAbdelMeguid   = U("wael.abdelmeguid@org.edu",   "Wael Abdel Meguid");
        // Theater
        var raniaSamir        = U("rania.samir@org.edu",        "Rania Samir");
        var hazemAshraf       = U("hazem.ashraf@org.edu",       "Hazem Ashraf");
        // Sports
        var tamerElNaggar     = U("tamer.elnaggar@org.edu",     "Tamer El Naggar");
        var dinaRaafat        = U("dina.raafat@org.edu",        "Dina Raafat");
        // AWG
        var saharElGendy      = U("sahar.elgendy@org.edu",      "Sahar El Gendy");
        var hassanMahmoud     = U("hassan.mahmoud@org.edu",     "Hassan Mahmoud");
        // Marketing & Outreach
        var lamiaYoussef      = U("lamia.youssef@org.edu",      "Lamia Youssef");
        var mahmoudFarouk     = U("mahmoud.farouk@org.edu",     "Mahmoud Farouk");
        // Admission Services
        var hebaAbdelAziz     = U("heba.abdelaziz@org.edu",     "Heba Abdel Aziz");
        var nermeenSami       = U("nermeen.sami@org.edu",       "Nermeen Sami");
        // Admission Office
        var waleedTantawy     = U("waleed.tantawy@org.edu",     "Waleed Tantawy");
        var ranaElKholy       = U("rana.elkholy@org.edu",       "Rana El Kholy");
        // Facility Management
        var mostafaRagab      = U("mostafa.ragab@org.edu",      "Mostafa Ragab");
        var samihaAdel        = U("samiha.adel@org.edu",        "Samiha Adel");
        // Security
        var abdallahRamzy     = U("abdallah.ramzy@org.edu",     "Abdallah Ramzy");
        var yasserGalal       = U("yasser.galal@org.edu",       "Yasser Galal");
        // Agriculture
        var nashwaAhmed       = U("nashwa.ahmed@org.edu",       "Nashwa Ahmed");
        var adelIsmail        = U("adel.ismail@org.edu",        "Adel Ismail");
        // Recruitment
        var mohamedAbdelWahab = U("mohamed.abdelwahab@org.edu", "Mohamed Abdel Wahab");
        var halaMahmoud       = U("hala.mahmoud@org.edu",       "Hala Mahmoud");
        // Compensation & Benefits
        var essamHamdy        = U("essam.hamdy@org.edu",        "Essam Hamdy");
        var imanElBatouty     = U("iman.elbatouty@org.edu",     "Iman El Batouty");
        // Personnel
        var sherifNaguib      = U("sherif.naguib@org.edu",      "Sherif Naguib");
        var amanyLotfy        = U("amany.lotfy@org.edu",        "Amany Lotfy");

        context.Users.AddRange(
            admin, chairman,
            ahmedMansour, moustafaFouad, marwaElSerafy, samiaElAshiry,
            mohamedIbrahim, radwaSelim, ghadirNassar, engyGalal, karimSalme, sherineKhalil, sherineSalamony,
            ohoudKhadr, yehiaRazzaz, amrBaibars, ibrahimKhalil, daliaElMainouny, aymanRahmou, salmaIbrahim,
            hananMostafa, tarekAbdelFattah, nohaElSayed, laylaHassan, ramyShawky, hossamBadawy, monaFarid,
            amiraSoliman, bassemYoussef, fatmaElZahraa, omarHashem, yasminAbdelRahman, khaledMostafa,
            nadiaFawzy, waelAbdelMeguid, raniaSamir, hazemAshraf, tamerElNaggar, dinaRaafat,
            saharElGendy, hassanMahmoud, lamiaYoussef, mahmoudFarouk, hebaAbdelAziz, nermeenSami,
            waleedTantawy, ranaElKholy, mostafaRagab, samihaAdel, abdallahRamzy, yasserGalal,
            nashwaAhmed, adelIsmail, mohamedAbdelWahab, halaMahmoud, essamHamdy, imanElBatouty,
            sherifNaguib, amanyLotfy
        );
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════
        //  COMMITTEES
        // ═══════════════════════════════════════════

        Committee C(string name, HierarchyLevel level, int? parentId = null)
            => new() { Name = name, HierarchyLevel = level, ParentCommitteeId = parentId, IsActive = true, CreatedAt = _now };

        // L0
        var topLevel = C("Top Level Committee", HierarchyLevel.TopLevel);
        context.Committees.Add(topLevel);
        await context.SaveChangesAsync();

        // L1
        var aqa              = C("Academic Quality & Accreditation", HierarchyLevel.Directors, topLevel.Id);
        var studentActivities = C("Student Activities",              HierarchyLevel.Directors, topLevel.Id);
        var admission        = C("Admission",                        HierarchyLevel.Directors, topLevel.Id);
        var campusAdmin      = C("Campus Administration",            HierarchyLevel.Directors, topLevel.Id);
        var hr               = C("HR",                               HierarchyLevel.Directors, topLevel.Id);
        context.Committees.AddRange(aqa, studentActivities, admission, campusAdmin, hr);
        await context.SaveChangesAsync();

        // L2 — under AQA
        var curriculum = C("Curriculum",                       HierarchyLevel.Functions, aqa.Id);
        var probation  = C("Probation & Mentoring",            HierarchyLevel.Functions, aqa.Id);
        var teaching   = C("Teaching and Evaluation Standard",  HierarchyLevel.Functions, aqa.Id);
        // L2 — under Student Activities
        var music   = C("Music",   HierarchyLevel.Functions, studentActivities.Id);
        var theater = C("Theater", HierarchyLevel.Functions, studentActivities.Id);
        var sports  = C("Sports",  HierarchyLevel.Functions, studentActivities.Id);
        var awg     = C("AWG",     HierarchyLevel.Functions, studentActivities.Id);
        // L2 — under Admission
        var marketing   = C("Marketing & Outreach", HierarchyLevel.Functions, admission.Id);
        var admServices = C("Admission Services",   HierarchyLevel.Functions, admission.Id);
        var admOffice   = C("Admission Office",     HierarchyLevel.Functions, admission.Id);
        // L2 — under Campus Administration
        var facility    = C("Facility Management", HierarchyLevel.Functions, campusAdmin.Id);
        var security    = C("Security",            HierarchyLevel.Functions, campusAdmin.Id);
        var agriculture = C("Agriculture",         HierarchyLevel.Functions, campusAdmin.Id);
        // L2 — under HR
        var recruitment  = C("Recruitment",             HierarchyLevel.Functions, hr.Id);
        var compBenefits = C("Compensation & Benefits", HierarchyLevel.Functions, hr.Id);
        var personnel    = C("Personnel",               HierarchyLevel.Functions, hr.Id);

        context.Committees.AddRange(
            curriculum, probation, teaching,
            music, theater, sports, awg,
            marketing, admServices, admOffice,
            facility, security, agriculture,
            recruitment, compBenefits, personnel
        );
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════
        //  MEMBERSHIPS
        // ═══════════════════════════════════════════

        var memberships = new List<CommitteeMembership>
        {
            // ── L0: Top Level Committee (7 heads) ──
            H(mohamedIbrahim, topLevel),
            H(radwaSelim, topLevel),
            H(ghadirNassar, topLevel),
            H(engyGalal, topLevel),
            H(karimSalme, topLevel),
            H(sherineKhalil, topLevel),
            H(sherineSalamony, topLevel),

            // ── L1: Academic Quality & Accreditation ──
            H(ghadirNassar, aqa),
            M(hananMostafa, aqa),
            M(tarekAbdelFattah, aqa),
            M(nohaElSayed, aqa),

            // ── L1: Student Activities ──
            H(sherineSalamony, studentActivities),
            M(ohoudKhadr, studentActivities),
            M(laylaHassan, studentActivities),
            M(yehiaRazzaz, studentActivities),

            // ── L1: Admission ──
            H(sherineKhalil, admission),
            M(ahmedMansour, admission),
            M(samiaElAshiry, admission),
            M(ramyShawky, admission),

            // ── L1: Campus Administration ──
            H(mohamedIbrahim, campusAdmin),
            M(amrBaibars, campusAdmin),
            M(ibrahimKhalil, campusAdmin),
            M(hossamBadawy, campusAdmin),
            M(monaFarid, campusAdmin),

            // ── L1: HR ──
            H(radwaSelim, hr),
            M(daliaElMainouny, hr),
            M(aymanRahmou, hr),
            M(salmaIbrahim, hr),

            // ── L2: Curriculum ──
            H(hananMostafa, curriculum),
            M(amiraSoliman, curriculum),
            M(bassemYoussef, curriculum),

            // ── L2: Probation & Mentoring ──
            H(tarekAbdelFattah, probation),
            M(fatmaElZahraa, probation),
            M(omarHashem, probation),

            // ── L2: Teaching and Evaluation Standard ──
            H(nohaElSayed, teaching),
            M(yasminAbdelRahman, teaching),
            M(khaledMostafa, teaching),

            // ── L2: Music ──
            H(ohoudKhadr, music),
            M(nadiaFawzy, music),
            M(waelAbdelMeguid, music),

            // ── L2: Theater ──
            H(laylaHassan, theater),
            M(raniaSamir, theater),
            M(hazemAshraf, theater),

            // ── L2: Sports ──
            H(yehiaRazzaz, sports),
            M(tamerElNaggar, sports),
            M(dinaRaafat, sports),

            // ── L2: AWG ──
            H(yehiaRazzaz, awg),
            M(saharElGendy, awg),
            M(hassanMahmoud, awg),

            // ── L2: Marketing & Outreach (2 co-heads) ──
            H(ahmedMansour, marketing),
            H(samiaElAshiry, marketing),
            M(lamiaYoussef, marketing),
            M(mahmoudFarouk, marketing),

            // ── L2: Admission Services ──
            H(ramyShawky, admServices),
            M(hebaAbdelAziz, admServices),
            M(nermeenSami, admServices),

            // ── L2: Admission Office ──
            H(sherineKhalil, admOffice),
            M(waleedTantawy, admOffice),
            M(ranaElKholy, admOffice),

            // ── L2: Facility Management (2 co-heads) ──
            H(amrBaibars, facility),
            H(ibrahimKhalil, facility),
            M(mostafaRagab, facility),
            M(samihaAdel, facility),

            // ── L2: Security ──
            H(hossamBadawy, security),
            M(abdallahRamzy, security),
            M(yasserGalal, security),

            // ── L2: Agriculture ──
            H(monaFarid, agriculture),
            M(nashwaAhmed, agriculture),
            M(adelIsmail, agriculture),

            // ── L2: Recruitment ──
            H(daliaElMainouny, recruitment),
            M(mohamedAbdelWahab, recruitment),
            M(halaMahmoud, recruitment),

            // ── L2: Compensation & Benefits ──
            H(aymanRahmou, compBenefits),
            M(essamHamdy, compBenefits),
            M(imanElBatouty, compBenefits),

            // ── L2: Personnel ──
            H(salmaIbrahim, personnel),
            M(sherifNaguib, personnel),
            M(amanyLotfy, personnel),
        };

        context.CommitteeMemberships.AddRange(memberships);
        await context.SaveChangesAsync();
    }
}
