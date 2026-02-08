using Microsoft.EntityFrameworkCore;
using ReportingSystem.Models;

namespace ReportingSystem.Data;

public static class OrganizationSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Committees.AnyAsync())
            return; // Already seeded

        var users = new Dictionary<string, User>();
        var now = DateTime.UtcNow;

        User U(string email, string name, string? title = null,
               SystemRole role = SystemRole.CommitteeUser, int? rank = null)
        {
            if (users.TryGetValue(email, out var existing)) return existing;
            var u = new User
            {
                Email = email, Name = name, Title = title,
                SystemRole = role, ChairmanOfficeRank = rank,
                IsActive = true, CreatedAt = now
            };
            users[email] = u;
            return u;
        }

        string E(string name)
        {
            var clean = name;
            foreach (var p in new[] { "Dr. ", "Prof. ", "Eng. ", "Mr. ", "Ms. ", "Mrs. ",
                                      "Atty. ", "Capt. ", "Chef " })
                if (clean.StartsWith(p)) { clean = clean[p.Length..]; break; }
            var parts = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var last = string.Join("", parts.Skip(1)).Replace("-", "").ToLower();
            return $"{char.ToLower(parts[0][0])}.{last}@org.edu";
        }

        // ── System Admin ──
        U("admin@org.edu", "System Administrator", "System Administrator", SystemRole.SystemAdmin);

        // ── Chairman ──
        var chairman = U("h.elsayed@org.edu", "Dr. Hassan El-Sayed", "Chairman & CEO", SystemRole.Chairman);

        // ── Chairman's Office ──
        var co1 = U("n.kamel@org.edu", "Nadia Kamel", "Chief of Staff", SystemRole.ChairmanOffice, 1);
        var co2 = U("t.mansour@org.edu", "Tarek Mansour", "Senior Executive Advisor", SystemRole.ChairmanOffice, 2);
        var co3 = U("l.abdelfattah@org.edu", "Layla Abdel-Fattah", "Executive Coordinator", SystemRole.ChairmanOffice, 3);
        var co4 = U("y.barakat@org.edu", "Youssef Barakat", "Executive Assistant", SystemRole.ChairmanOffice, 4);

        // ── L0 Members ──
        var amira  = U(E("Prof. Amira Shalaby"),  "Prof. Amira Shalaby",  "GS — Academic Affairs");
        var khaled = U(E("Eng. Khaled Mostafa"),   "Eng. Khaled Mostafa",  "GS — Administration & Operations");
        var mariam = U(E("Dr. Mariam Fawzy"),      "Dr. Mariam Fawzy",     "GS — Technology & Innovation");
        var adel   = U(E("Mr. Adel Soliman"),      "Mr. Adel Soliman",     "GS — Finance & Governance");
        var heba   = U(E("Dr. Heba Nasser"),       "Dr. Heba Nasser",      "GS — Student Experience & Services");

        // ── L0 Shadows ──
        var shAmira  = U(E("Dr. Rania Ismail"),    "Dr. Rania Ismail",     "Shadow — Academic Affairs");
        var shKhaled = U(E("Eng. Sameh Abdou"),    "Eng. Sameh Abdou",     "Shadow — Administration & Operations");
        var shMariam = U(E("Eng. Omar Rashid"),    "Eng. Omar Rashid",     "Shadow — Technology & Innovation");
        var shAdel   = U(E("Ms. Dina Hafez"),      "Ms. Dina Hafez",       "Shadow — Finance & Governance");
        var shHeba   = U(E("Ms. Salma El-Gohary"), "Ms. Salma El-Gohary",  "Shadow — Student Experience & Services");

        // ─────────── SECTOR 1: Academic Affairs ───────────

        // L1 Directors
        var faridZaki    = U(E("Dr. Farid Zaki"),    "Dr. Farid Zaki",    "Director, Academic Programs");
        var monaHelal    = U(E("Dr. Mona Helal"),    "Dr. Mona Helal",    "Deputy Director, Academic Programs");
        var aymanTawfik  = U(E("Dr. Ayman Tawfik"),  "Dr. Ayman Tawfik",  "Director, Research & Graduate Studies");
        var samiraLotfi  = U(E("Dr. Samira Lotfi"),  "Dr. Samira Lotfi",  "Deputy Director, Research & Graduate Studies");
        var nohaMahmoud  = U(E("Dr. Noha Mahmoud"),  "Dr. Noha Mahmoud",  "Director, Academic Quality & Accreditation");
        var waelKassem   = U(E("Dr. Wael Kassem"),   "Dr. Wael Kassem",   "Deputy Director, Academic Quality");
        var ranaShaheen  = U(E("Ms. Rana Shaheen"),  "Ms. Rana Shaheen",  "Director, Library & Learning Resources");

        // L2 Members — Academic Programs
        var lamiaRefaat  = U(E("Dr. Lamia Refaat"),  "Dr. Lamia Refaat",  "Curriculum Development");
        var alaaBadawi   = U(E("Dr. Alaa Badawi"),   "Dr. Alaa Badawi",   "E-Learning & Digital Education");
        var wissamKhoury = U(E("Mr. Wissam Khoury"), "Mr. Wissam Khoury", "Curriculum Development");
        var yasserNasr   = U(E("Dr. Yasser Nasr"),   "Dr. Yasser Nasr",   "Teaching & Faculty Affairs");
        var shahiraTalaat= U(E("Dr. Shahira Talaat"),"Dr. Shahira Talaat","Teaching & Faculty Affairs");
        var reemAtef     = U(E("Ms. Reem Atef"),     "Ms. Reem Atef",     "Teaching & Faculty Affairs");
        var nabilHamed   = U(E("Dr. Nabil Hamed"),   "Dr. Nabil Hamed",   "Teaching & Faculty Affairs");
        var soniaGuirguis= U(E("Dr. Sonia Guirguis"),"Dr. Sonia Guirguis","Examination & Assessment");
        var hatemBarsoum = U(E("Mr. Hatem Barsoum"), "Mr. Hatem Barsoum", "Examination & Assessment");
        var nevineSaad   = U(E("Ms. Nevine Saad"),   "Ms. Nevine Saad",   "Examination & Assessment");
        var rafikHabib   = U(E("Mr. Rafik Habib"),   "Mr. Rafik Habib",   "Examination & Assessment");
        var magedBotros  = U(E("Eng. Maged Botros"), "Eng. Maged Botros", "E-Learning");
        var hodaLabib    = U(E("Ms. Hoda Labib"),    "Ms. Hoda Labib",    "E-Learning");
        var peterAziz    = U(E("Mr. Peter Aziz"),    "Mr. Peter Aziz",    "E-Learning");

        // L2 Members — Research & Graduate Studies
        var nabilaFikry  = U(E("Dr. Nabila Fikry"),  "Dr. Nabila Fikry",  "Research Grants & IP");
        var ihabSerag    = U(E("Mr. Ihab Serag"),    "Mr. Ihab Serag",    "Research Grants");
        var yaraMagdy    = U(E("Ms. Yara Magdy"),    "Ms. Yara Magdy",    "Research Grants");
        var walidDarwish = U(E("Dr. Walid Darwish"), "Dr. Walid Darwish", "Graduate Programs");
        var asmaaRagab   = U(E("Dr. Asmaa Ragab"),  "Dr. Asmaa Ragab",   "Graduate Programs");
        var dinaShaker   = U(E("Ms. Dina Shaker"),  "Ms. Dina Shaker",   "Graduate Programs");
        var bassemKhalaf = U(E("Mr. Bassem Khalaf"), "Mr. Bassem Khalaf", "Graduate Programs");
        var heshamAnwar  = U(E("Dr. Hesham Anwar"),  "Dr. Hesham Anwar",  "IP & Publications");
        var sallyGerges  = U(E("Ms. Sally Gerges"),  "Ms. Sally Gerges",  "IP & Publications");
        var ramySalib    = U(E("Mr. Ramy Salib"),    "Mr. Ramy Salib",    "IP & Publications");

        // L2 Members — Academic Quality
        var manalRizk    = U(E("Dr. Manal Rizk"),    "Dr. Manal Rizk",    "Quality Assurance");
        var saharFouad   = U(E("Ms. Sahar Fouad"),   "Ms. Sahar Fouad",   "Quality Assurance");
        var kareemMourad = U(E("Mr. Kareem Mourad"), "Mr. Kareem Mourad", "Quality Assurance");
        var ossamaFathy  = U(E("Dr. Ossama Fathy"),  "Dr. Ossama Fathy",  "Institutional Accreditation");
        var mervatKamal  = U(E("Ms. Mervat Kamal"),  "Ms. Mervat Kamal",  "Institutional Accreditation");
        var amrHennawy   = U(E("Mr. Amr Hennawy"),   "Mr. Amr Hennawy",   "Institutional Accreditation");
        var abeerNazif   = U(E("Dr. Abeer Nazif"),   "Dr. Abeer Nazif",   "Academic Program Reviews");
        var nagwaAttia   = U(E("Ms. Nagwa Attia"),   "Ms. Nagwa Attia",   "Academic Program Reviews");
        var ashrafHanna  = U(E("Mr. Ashraf Hanna"),  "Mr. Ashraf Hanna",  "Academic Program Reviews");

        // L2 Members — Library
        var lamisHalim   = U(E("Ms. Lamis Halim"),   "Ms. Lamis Halim",   "Collection Management");
        var amirFarag    = U(E("Mr. Amir Farag"),    "Mr. Amir Farag",    "Digital Library Services");
        var suzyBeshay   = U(E("Ms. Suzy Beshay"),   "Ms. Suzy Beshay",   "Collection Management");
        var minaGuirguis = U(E("Eng. Mina Guirguis"),"Eng. Mina Guirguis","Digital Library Services");
        var hebaSamir    = U(E("Ms. Heba Samir"),    "Ms. Heba Samir",    "Digital Library Services");
        var georgeYoussef= U(E("Mr. George Youssef"),"Mr. George Youssef","Digital Library Services");
        var bassemNakhla = U(E("Mr. Bassem Nakhla"), "Mr. Bassem Nakhla", "Reading Halls");
        var christineAdel= U(E("Ms. Christine Adel"),"Ms. Christine Adel","Reading Halls");

        // ─────────── SECTOR 2: Administration & Operations ───────────

        // L1 Directors
        var mahmoudGabr  = U(E("Eng. Mahmoud Gabr"), "Eng. Mahmoud Gabr", "Director, Facilities & Maintenance");
        var ashrafYoussef= U(E("Eng. Ashraf Youssef"),"Eng. Ashraf Youssef","Deputy Director, Facilities");
        var faridaAmin   = U(E("Ms. Farida Amin"),   "Ms. Farida Amin",   "Director, Human Resources");
        var ahmedTaha    = U(E("Mr. Ahmed Taha"),     "Mr. Ahmed Taha",    "Deputy Director, Human Resources");
        var sherifNaguib = U(E("Mr. Sherif Naguib"),  "Mr. Sherif Naguib", "Director, Procurement & Supply Chain");
        var waleedHamdi  = U(E("Chef Waleed Hamdi"), "Chef Waleed Hamdi", "Director, Food & Beverage");
        var hanaMorsi    = U(E("Ms. Hana Morsi"),    "Ms. Hana Morsi",    "Deputy Director, Food & Beverage");

        // L2 Members — Facilities
        var saadHashem   = U(E("Mr. Saad Hashem"),   "Mr. Saad Hashem",   "Building Maintenance");
        var fadiMilad    = U(E("Mr. Fadi Milad"),    "Mr. Fadi Milad",    "Building Maintenance");
        var nabilGuindi  = U(E("Mr. Nabil Guindi"),  "Mr. Nabil Guindi",  "Building Maintenance");
        var magdyBoulos  = U(E("Mr. Magdy Boulos"),  "Mr. Magdy Boulos",  "Landscaping");
        var nawalSami    = U(E("Ms. Nawal Sami"),    "Ms. Nawal Sami",    "Landscaping");
        var rafatHanna   = U(E("Mr. Rafat Hanna"),   "Mr. Rafat Hanna",   "Landscaping");
        var haniTawfik   = U(E("Eng. Hani Tawfik"),  "Eng. Hani Tawfik",  "Utilities & Energy");
        var mohsenLabib  = U(E("Eng. Mohsen Labib"), "Eng. Mohsen Labib", "Utilities & Energy");
        var sherifWaguih = U(E("Mr. Sherif Waguih"), "Mr. Sherif Waguih", "Utilities & Energy");
        var mayRaouf     = U(E("Ms. May Raouf"),     "Ms. May Raouf",     "Utilities & Energy");
        var redaMostafa  = U(E("Capt. Reda Mostafa"),"Capt. Reda Mostafa","Safety & Security");
        var samehGirgis  = U(E("Mr. Sameh Girgis"),  "Mr. Sameh Girgis",  "Safety & Security");
        var emadBarsoum  = U(E("Mr. Emad Barsoum"),  "Mr. Emad Barsoum",  "Safety & Security");
        var waelYoussef  = U(E("Mr. Wael Youssef"),  "Mr. Wael Youssef",  "Safety & Security");

        // L2 Members — HR
        var manarEssam   = U(E("Ms. Manar Essam"),   "Ms. Manar Essam",   "Recruitment");
        var saraMourad   = U(E("Ms. Sara Mourad"),   "Ms. Sara Mourad",   "Recruitment");
        var hossamAdly   = U(E("Mr. Hossam Adly"),   "Mr. Hossam Adly",   "Employee Relations");
        var nohaElKady   = U(E("Ms. Noha El-Kady"),  "Ms. Noha El-Kady",  "Payroll & Compensation");
        var amgadIshak   = U(E("Mr. Amgad Ishak"),   "Mr. Amgad Ishak",   "Payroll & Compensation");
        var vivianLabib  = U(E("Ms. Vivian Labib"),   "Ms. Vivian Labib",  "Payroll & Compensation");
        var bassemThabet = U(E("Mr. Bassem Thabet"), "Mr. Bassem Thabet", "Payroll & Compensation");
        var marwaSelim   = U(E("Ms. Marwa Selim"),   "Ms. Marwa Selim",   "Training & Development");
        var ehabMikhail  = U(E("Dr. Ehab Mikhail"),  "Dr. Ehab Mikhail",  "Training & Development");
        var nerminAziz   = U(E("Ms. Nermin Aziz"),   "Ms. Nermin Aziz",   "Training & Development");
        var fadyHabib    = U(E("Mr. Fady Habib"),     "Mr. Fady Habib",    "Training & Development");
        var hendBadr     = U(E("Ms. Hend Badr"),     "Ms. Hend Badr",     "Employee Relations");
        var wagdyFarid   = U(E("Mr. Wagdy Farid"),   "Mr. Wagdy Farid",   "Employee Relations");
        var rashaGuirguis= U(E("Ms. Rasha Guirguis"),"Ms. Rasha Guirguis","Employee Relations");

        // L2 Members — Procurement
        var aminaLotfy   = U(E("Ms. Amina Lotfy"),   "Ms. Amina Lotfy",   "Purchasing & Sourcing");
        var ehabSedky    = U(E("Mr. Ehab Sedky"),    "Mr. Ehab Sedky",    "Inventory & Warehouse");
        var nadiaFouad   = U(E("Ms. Nadia Fouad"),   "Ms. Nadia Fouad",   "Vendor Management");
        var magdyAbdallah= U(E("Mr. Magdy Abdallah"),"Mr. Magdy Abdallah","Inventory");
        var salwaRizkalla= U(E("Ms. Salwa Rizkalla"),"Ms. Salwa Rizkalla","Inventory");
        var aymanHanna   = U(E("Mr. Ayman Hanna"),   "Mr. Ayman Hanna",   "Inventory");
        var tarekGhattas = U(E("Mr. Tarek Ghattas"), "Mr. Tarek Ghattas", "Purchasing");
        var hodaMalak    = U(E("Ms. Hoda Malak"),    "Ms. Hoda Malak",    "Purchasing");
        var gergesFahmy  = U(E("Mr. Gerges Fahmy"),  "Mr. Gerges Fahmy",  "Purchasing");

        // L2 Members — Food & Beverage
        var ramezHabib   = U(E("Mr. Ramez Habib"),   "Mr. Ramez Habib",   "Catering Operations");
        var yvonneBoulos = U(E("Ms. Yvonne Boulos"), "Ms. Yvonne Boulos", "Food Safety & Hygiene");
        var shadyMounir  = U(E("Mr. Shady Mounir"),  "Mr. Shady Mounir",  "Catering Operations");
        var ramiSaleh    = U(E("Chef Rami Saleh"),   "Chef Rami Saleh",   "Kitchen Management");
        var jacklineSalib= U(E("Ms. Jackline Salib"),"Ms. Jackline Salib","Kitchen Management");
        var waelHabashi  = U(E("Mr. Wael Habashi"),  "Mr. Wael Habashi",  "Kitchen Management");
        var mariamGuindi = U(E("Ms. Mariam Guindi"), "Ms. Mariam Guindi", "Cafeteria & Retail");
        var peterNassif  = U(E("Mr. Peter Nassif"),  "Mr. Peter Nassif",  "Cafeteria & Retail");
        var dinaBasta    = U(E("Ms. Dina Basta"),    "Ms. Dina Basta",    "Cafeteria & Retail");
        var fadyMikhail  = U(E("Mr. Fady Mikhail"),  "Mr. Fady Mikhail",  "Food Safety");
        var ireneHabib   = U(E("Ms. Irene Habib"),   "Ms. Irene Habib",   "Food Safety");
        var samehNazmi   = U(E("Mr. Sameh Nazmi"),   "Mr. Sameh Nazmi",   "Food Safety");

        // ─────────── SECTOR 3: Technology & Innovation ───────────

        // L1 Directors
        var ibrahimHassan= U(E("Eng. Ibrahim Hassan"),"Eng. Ibrahim Hassan","Director, Software Development");
        var yasminFarouk = U(E("Eng. Yasmin Farouk"),"Eng. Yasmin Farouk","Deputy Director, Software Development");
        var bassemGad    = U(E("Eng. Bassem Gad"),   "Eng. Bassem Gad",   "Director, IT Infrastructure & Networks");
        var karimTawfik  = U(E("Eng. Karim Tawfik"), "Eng. Karim Tawfik", "Deputy Director, IT Infrastructure");
        var hazemRizk    = U(E("Mr. Hazem Rizk"),    "Mr. Hazem Rizk",    "Director, Tech Support & Help Desk");
        var mahaSamir    = U(E("Ms. Maha Samir"),    "Ms. Maha Samir",    "Deputy Director, Tech Support");

        // L2 Members — Software Dev
        var tarekBotros  = U(E("Eng. Tarek Botros"), "Eng. Tarek Botros", "Web & Mobile Development");
        var sandraNassif = U(E("Eng. Sandra Nassif"),"Eng. Sandra Nassif","QA & Software Testing");
        var bahaaSamy    = U(E("Mr. Bahaa Samy"),    "Mr. Bahaa Samy",    "Web Applications");
        var marinaSedky  = U(E("Eng. Marina Sedky"), "Eng. Marina Sedky", "Mobile Development");
        var sherifBeshay = U(E("Mr. Sherif Beshay"), "Mr. Sherif Beshay", "Mobile Development");
        var reemMikhail  = U(E("Ms. Reem Mikhail"),  "Ms. Reem Mikhail",  "Mobile Development");
        var ahmedGaber   = U(E("Eng. Ahmed Gaber"),  "Eng. Ahmed Gaber",  "Enterprise Systems");
        var monicaHalim  = U(E("Eng. Monica Halim"), "Eng. Monica Halim", "Enterprise Systems");
        var abanobSobhy  = U(E("Mr. Abanob Sobhy"),  "Mr. Abanob Sobhy",  "Enterprise Systems");
        var karimWagdy   = U(E("Eng. Karim Wagdy"),  "Eng. Karim Wagdy",  "Enterprise Systems");
        var johnLabib    = U(E("Mr. John Labib"),     "Mr. John Labib",    "QA & Testing");
        var marianFarid  = U(E("Ms. Marian Farid"),  "Ms. Marian Farid",  "QA & Testing");
        var emadYoussef  = U(E("Mr. Emad Youssef"),  "Mr. Emad Youssef",  "QA & Testing");

        // L2 Members — IT Infrastructure
        var bishoyAziz   = U(E("Eng. Bishoy Aziz"),  "Eng. Bishoy Aziz",  "Network Administration");
        var hanyFarouk   = U(E("Mr. Hany Farouk"),   "Mr. Hany Farouk",   "Network Administration");
        var rafikIshak   = U(E("Mr. Rafik Ishak"),   "Mr. Rafik Ishak",   "Network Administration");
        var markGabra    = U(E("Eng. Mark Gabra"),   "Eng. Mark Gabra",   "Cybersecurity");
        var waelSamir    = U(E("Mr. Wael Samir"),    "Mr. Wael Samir",    "Server & Cloud");
        var nadiaGuirguis= U(E("Ms. Nadia Guirguis"),"Ms. Nadia Guirguis","Server & Cloud");
        var seifHaroun   = U(E("Mr. Seif Haroun"),   "Mr. Seif Haroun",   "Cybersecurity");
        var mirnaLabib   = U(E("Ms. Mirna Labib"),   "Ms. Mirna Labib",   "Cybersecurity");
        var andrewNassif = U(E("Mr. Andrew Nassif"),  "Mr. Andrew Nassif", "Cybersecurity");

        // L2 Members — Tech Support
        var michaelAdel  = U(E("Mr. Michael Adel"),  "Mr. Michael Adel",  "End-User Support");
        var randaThabet  = U(E("Ms. Randa Thabet"),  "Ms. Randa Thabet",  "End-User Support");
        var rafikFarag   = U(E("Mr. Rafik Farag"),   "Mr. Rafik Farag",   "AV & Smart Classroom");
        var ehabGuirguis = U(E("Mr. Ehab Guirguis"), "Mr. Ehab Guirguis", "Hardware Management");
        var nancySalib   = U(E("Ms. Nancy Salib"),   "Ms. Nancy Salib",   "Hardware Management");
        var aymanHabib   = U(E("Mr. Ayman Habib"),   "Mr. Ayman Habib",   "Hardware Management");
        var georgeHabib  = U(E("Mr. George Habib"),  "Mr. George Habib",  "AV Support");
        var carolineNaguib=U(E("Ms. Caroline Naguib"),"Ms. Caroline Naguib","AV Support");
        var naderIshak   = U(E("Mr. Nader Ishak"),   "Mr. Nader Ishak",   "AV Support");

        // ─────────── SECTOR 4: Finance & Governance ───────────

        // L1 Directors
        var heshamFarag  = U(E("Mr. Hesham Farag"),  "Mr. Hesham Farag",  "Director, Finance & Accounting");
        var nermeenKhalil= U(E("Ms. Nermeen Khalil"),"Ms. Nermeen Khalil","Deputy Director, Finance & Accounting");
        var gamalReda    = U(E("Atty. Gamal Reda"),  "Atty. Gamal Reda",  "Director, Legal & Compliance");
        var mostafaSalem = U(E("Mr. Mostafa Salem"), "Mr. Mostafa Salem", "Director, Internal Audit");
        var emanLotfy    = U(E("Ms. Eman Lotfy"),    "Ms. Eman Lotfy",    "Deputy Director, Internal Audit");
        var sawsanOsman  = U(E("Dr. Sawsan Osman"),  "Dr. Sawsan Osman",  "Director, Strategic Planning");

        // L2 Members — Finance
        var wagdyLabib   = U(E("Mr. Wagdy Labib"),   "Mr. Wagdy Labib",   "General Accounting");
        var vivianSamir  = U(E("Ms. Vivian Samir"),  "Ms. Vivian Samir",  "General Accounting");
        var ehabNakhla   = U(E("Mr. Ehab Nakhla"),   "Mr. Ehab Nakhla",   "General Accounting");
        var nagwaSedky   = U(E("Ms. Nagwa Sedky"),   "Ms. Nagwa Sedky",   "Budgeting");
        var amrTawfik    = U(E("Mr. Amr Tawfik"),    "Mr. Amr Tawfik",    "Budgeting & Treasury");
        var hendFarouk   = U(E("Ms. Hend Farouk"),   "Ms. Hend Farouk",   "Budgeting");
        var ashrafHabib  = U(E("Mr. Ashraf Habib"),  "Mr. Ashraf Habib",  "Tuition & Fees");
        var sallyGuirguis= U(E("Ms. Sally Guirguis"),"Ms. Sally Guirguis","Tuition & Fees");
        var magedAziz    = U(E("Mr. Maged Aziz"),    "Mr. Maged Aziz",    "Tuition & Fees");
        var lamiaFarag   = U(E("Ms. Lamia Farag"),   "Ms. Lamia Farag",   "Treasury");
        var ramyMounir   = U(E("Mr. Ramy Mounir"),   "Mr. Ramy Mounir",   "Treasury");

        // L2 Members — Legal
        var hodaShaker   = U(E("Atty. Hoda Shaker"), "Atty. Hoda Shaker", "Regulatory Compliance");
        var bassemNaguib = U(E("Mr. Bassem Naguib"), "Mr. Bassem Naguib", "Contracts");
        var amalHabib    = U(E("Ms. Amal Habib"),    "Ms. Amal Habib",    "Regulatory Compliance");
        var sherifLabib  = U(E("Mr. Sherif Labib"),  "Mr. Sherif Labib",  "Regulatory Compliance");
        var raniaGuindi  = U(E("Ms. Rania Guindi"),  "Ms. Rania Guindi",  "Regulatory Compliance");
        var mohamedBadr  = U(E("Atty. Mohamed Badr"),"Atty. Mohamed Badr","Dispute Resolution");
        var saraYoussef  = U(E("Atty. Sara Youssef"),"Atty. Sara Youssef","Dispute Resolution");
        var naderMikhail = U(E("Mr. Nader Mikhail"), "Mr. Nader Mikhail", "Dispute Resolution");

        // L2 Members — Audit
        var magdyAziz    = U(E("Mr. Magdy Aziz"),    "Mr. Magdy Aziz",    "Financial Audit");
        var lamisFarid   = U(E("Ms. Lamis Farid"),   "Ms. Lamis Farid",   "Financial Audit");
        var ihabLabib    = U(E("Mr. Ihab Labib"),     "Mr. Ihab Labib",    "Financial Audit");
        var shereenHabib = U(E("Ms. Shereen Habib"), "Ms. Shereen Habib", "Operational Audit");
        var waelNaguib   = U(E("Mr. Wael Naguib"),   "Mr. Wael Naguib",   "Operational Audit");
        var mervatSalib  = U(E("Ms. Mervat Salib"),  "Ms. Mervat Salib",  "Operational Audit");
        var amirSedky    = U(E("Mr. Amir Sedky"),    "Mr. Amir Sedky",    "Compliance Audit");
        var nerminHanna  = U(E("Ms. Nermin Hanna"),  "Ms. Nermin Hanna",  "Compliance Audit");

        // L2 Members — Strategic Planning
        var amrNazif     = U(E("Dr. Amr Nazif"),     "Dr. Amr Nazif",     "Institutional Strategy");
        var reemFarag    = U(E("Ms. Reem Farag"),    "Ms. Reem Farag",    "Strategy & KPIs");
        var hazemSalib   = U(E("Mr. Hazem Salib"),   "Mr. Hazem Salib",   "Strategy & KPIs");
        var shahiraLabib = U(E("Ms. Shahira Labib"), "Ms. Shahira Labib", "Performance Monitoring");
        var karimHabib   = U(E("Mr. Karim Habib"),   "Mr. Karim Habib",   "Performance Monitoring");
        var bassemFarid  = U(E("Mr. Bassem Farid"),  "Mr. Bassem Farid",  "Organizational Development");
        var dinaIshak    = U(E("Ms. Dina Ishak"),    "Ms. Dina Ishak",    "Organizational Development");
        var waelSedky    = U(E("Mr. Wael Sedky"),    "Mr. Wael Sedky",    "Organizational Development");

        // ─────────── SECTOR 5: Student Experience & Services ───────────

        // L1 Directors
        var amalFathi    = U(E("Dr. Amal Fathi"),    "Dr. Amal Fathi",    "Director, Student Affairs");
        var tamerElSisi  = U(E("Mr. Tamer El-Sisi"), "Mr. Tamer El-Sisi", "Deputy Director, Student Affairs");
        var halaShafik   = U(E("Dr. Hala Shafik"),   "Dr. Hala Shafik",   "Director, Academic Support & Tutoring");
        var karimAbdallah= U(E("Mr. Karim Abdallah"),"Mr. Karim Abdallah","Director, Student Activities");
        var daliaNour    = U(E("Ms. Dalia Nour"),    "Ms. Dalia Nour",    "Deputy Director, Student Activities");
        var ghadaMohsen  = U(E("Ms. Ghada Mohsen"),  "Ms. Ghada Mohsen",  "Director, Career Services & Alumni");

        // L2 Members — Student Affairs
        var azzaNaguib   = U(E("Ms. Azza Naguib"),   "Ms. Azza Naguib",   "Admissions & Registration");
        var hatemFarid   = U(E("Mr. Hatem Farid"),   "Mr. Hatem Farid",   "Admissions & Registration");
        var randaSalib   = U(E("Ms. Randa Salib"),   "Ms. Randa Salib",   "Admissions & Registration");
        var aymanLabib   = U(E("Mr. Ayman Labib"),   "Mr. Ayman Labib",   "Student Records");
        var hodaHabib    = U(E("Ms. Hoda Habib"),    "Ms. Hoda Habib",    "Student Records");
        var sherifSamir  = U(E("Mr. Sherif Samir"),  "Mr. Sherif Samir",  "Student Records");
        var niveenAdel   = U(E("Dr. Niveen Adel"),   "Dr. Niveen Adel",   "Student Welfare & Counseling");
        var hendSalib    = U(E("Ms. Hend Salib"),    "Ms. Hend Salib",    "Student Welfare");
        var fadyNaguib   = U(E("Mr. Fady Naguib"),   "Mr. Fady Naguib",   "Student Welfare");
        var mariamHabib  = U(E("Ms. Mariam Habib"),  "Ms. Mariam Habib",  "Student Welfare");
        var dinaFarouk   = U(E("Ms. Dina Farouk"),   "Ms. Dina Farouk",   "International Students Office");
        var andrewLabib  = U(E("Mr. Andrew Labib"),  "Mr. Andrew Labib",  "International Students");
        var mahaIshak    = U(E("Ms. Maha Ishak"),    "Ms. Maha Ishak",    "International Students");

        // L2 Members — Academic Support
        var bassemGuirguis=U(E("Mr. Bassem Guirguis"),"Mr. Bassem Guirguis","Tutoring");
        var reemSalib    = U(E("Ms. Reem Salib"),    "Ms. Reem Salib",    "Tutoring");
        var nancyHabib   = U(E("Ms. Nancy Habib"),   "Ms. Nancy Habib",   "Tutoring");
        var ranaAdel     = U(E("Dr. Rana Adel"),     "Dr. Rana Adel",     "Academic Advising");
        var saharNaguib  = U(E("Ms. Sahar Naguib"),  "Ms. Sahar Naguib",  "Academic Advising & Study Skills");
        var emadLabib    = U(E("Mr. Emad Labib"),    "Mr. Emad Labib",    "Academic Advising");
        var mirnaFarid   = U(E("Ms. Mirna Farid"),   "Ms. Mirna Farid",   "Academic Advising");
        var kareemSalib  = U(E("Mr. Kareem Salib"),  "Mr. Kareem Salib",  "Study Skills");
        var hodaNaguib   = U(E("Ms. Hoda Naguib"),   "Ms. Hoda Naguib",   "Study Skills");

        // L2 Members — Student Activities
        var omarLabib    = U(E("Mr. Omar Labib"),    "Mr. Omar Labib",    "Clubs & Organizations");
        var yaraHabib    = U(E("Ms. Yara Habib"),    "Ms. Yara Habib",    "Cultural Events");
        var ramySalib2   = U(E("Mr. Ramy Salib"),    "Mr. Ramy Salib",    "Clubs"); // already exists, reuse
        var alaaFarid    = U(E("Mr. Alaa Farid"),    "Mr. Alaa Farid",    "Sports & Recreation");
        var nadiaHabib   = U(E("Ms. Nadia Habib"),   "Ms. Nadia Habib",   "Sports & Recreation");
        var waelGuirguis = U(E("Mr. Wael Guirguis"), "Mr. Wael Guirguis", "Sports & Recreation");
        var bassemSamir  = U(E("Mr. Bassem Samir"),  "Mr. Bassem Samir",  "Cultural Events");
        var lamisNaguib  = U(E("Ms. Lamis Naguib"),  "Ms. Lamis Naguib",  "Cultural Events");
        var ranaHabib    = U(E("Ms. Rana Habib"),    "Ms. Rana Habib",    "Cultural Events");

        // L2 Members — Career Services
        var reemLabib    = U(E("Ms. Reem Labib"),    "Ms. Reem Labib",    "Internship & Co-op Programs");
        var hossamFarid  = U(E("Mr. Hossam Farid"),  "Mr. Hossam Farid",  "Alumni Relations");
        var sallyHabib   = U(E("Ms. Sally Habib"),   "Ms. Sally Habib",   "Career Counseling");
        var tamerGuirguis= U(E("Mr. Tamer Guirguis"),"Mr. Tamer Guirguis","Internship Programs");
        var hodaFarid    = U(E("Ms. Hoda Farid"),    "Ms. Hoda Farid",    "Internship Programs");
        var amrSalib     = U(E("Mr. Amr Salib"),     "Mr. Amr Salib",     "Internship Programs");
        var mahaLabib    = U(E("Ms. Maha Labib"),    "Ms. Maha Labib",    "Alumni Network");
        var georgeNaguib = U(E("Mr. George Naguib"), "Mr. George Naguib", "Alumni Network");
        var dinaHabib    = U(E("Ms. Dina Habib"),    "Ms. Dina Habib",    "Alumni Network");

        // ── Save all users ──
        context.Users.AddRange(users.Values);
        await context.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════
        // COMMITTEES
        // ══════════════════════════════════════════════════════════

        var s1 = "Academic Affairs";
        var s2 = "Administration & Operations";
        var s3 = "Technology & Innovation";
        var s4 = "Finance & Governance";
        var s5 = "Student Experience & Services";

        Committee C(string name, HierarchyLevel lvl, Committee? parent = null, string? sector = null)
        {
            var c = new Committee
            {
                Name = name, HierarchyLevel = lvl,
                ParentCommittee = parent, Sector = sector,
                IsActive = true, CreatedAt = now
            };
            context.Committees.Add(c);
            return c;
        }

        // ── L0 ──
        var topLevel = C("Top Level Committee", HierarchyLevel.TopLevel);

        // ── L1 — Sector 1 ──
        var acadPrograms  = C("Academic Programs Directorate", HierarchyLevel.Directors, topLevel, s1);
        var researchGrad  = C("Research & Graduate Studies Directorate", HierarchyLevel.Directors, topLevel, s1);
        var acadQuality   = C("Academic Quality & Accreditation Directorate", HierarchyLevel.Directors, topLevel, s1);
        var libraryRes    = C("Library & Learning Resources Directorate", HierarchyLevel.Directors, topLevel, s1);

        // ── L1 — Sector 2 ──
        var facilities    = C("Facilities & Maintenance Directorate", HierarchyLevel.Directors, topLevel, s2);
        var humanRes      = C("Human Resources Directorate", HierarchyLevel.Directors, topLevel, s2);
        var procurement   = C("Procurement & Supply Chain Directorate", HierarchyLevel.Directors, topLevel, s2);
        var foodBev       = C("Food & Beverage Directorate", HierarchyLevel.Directors, topLevel, s2);

        // ── L1 — Sector 3 ──
        var softwareDev   = C("Software Development Directorate", HierarchyLevel.Directors, topLevel, s3);
        var itInfra       = C("IT Infrastructure & Networks Directorate", HierarchyLevel.Directors, topLevel, s3);
        var techSupport   = C("Tech Support & Help Desk Directorate", HierarchyLevel.Directors, topLevel, s3);

        // ── L1 — Sector 4 ──
        var financeAcct   = C("Finance & Accounting Directorate", HierarchyLevel.Directors, topLevel, s4);
        var legalCompl    = C("Legal & Compliance Directorate", HierarchyLevel.Directors, topLevel, s4);
        var internalAudit = C("Internal Audit Directorate", HierarchyLevel.Directors, topLevel, s4);
        var stratPlan     = C("Strategic Planning Directorate", HierarchyLevel.Directors, topLevel, s4);

        // ── L1 — Sector 5 ──
        var studentAffairs= C("Student Affairs Directorate", HierarchyLevel.Directors, topLevel, s5);
        var acadSupport   = C("Academic Support & Tutoring Directorate", HierarchyLevel.Directors, topLevel, s5);
        var studentAct    = C("Student Activities & Engagement Directorate", HierarchyLevel.Directors, topLevel, s5);
        var careerServices= C("Career Services & Alumni Relations Directorate", HierarchyLevel.Directors, topLevel, s5);

        // ── L2 — Academic Programs ──
        var currDev       = C("Curriculum Development", HierarchyLevel.Functions, acadPrograms, s1);
        var teachFaculty  = C("Teaching & Faculty Affairs", HierarchyLevel.Functions, acadPrograms, s1);
        var examAssess    = C("Examination & Assessment", HierarchyLevel.Functions, acadPrograms, s1);
        var eLearning     = C("E-Learning & Digital Education", HierarchyLevel.Functions, acadPrograms, s1);

        // ── L2 — Research & Graduate Studies ──
        var resGrants     = C("Research Grants & Funding", HierarchyLevel.Functions, researchGrad, s1);
        var gradPrograms  = C("Graduate Programs Administration", HierarchyLevel.Functions, researchGrad, s1);
        var ipPub         = C("Intellectual Property & Publications", HierarchyLevel.Functions, researchGrad, s1);

        // ── L2 — Academic Quality ──
        var qaStandards   = C("Quality Assurance & Standards", HierarchyLevel.Functions, acadQuality, s1);
        var instAccred    = C("Institutional Accreditation", HierarchyLevel.Functions, acadQuality, s1);
        var acadReviews   = C("Academic Program Reviews", HierarchyLevel.Functions, acadQuality, s1);

        // ── L2 — Library ──
        var collMgmt      = C("Collection Management & Acquisition", HierarchyLevel.Functions, libraryRes, s1);
        var digLibrary    = C("Digital Library Services", HierarchyLevel.Functions, libraryRes, s1);
        var readingHalls  = C("Reading Halls & Study Spaces", HierarchyLevel.Functions, libraryRes, s1);

        // ── L2 — Facilities ──
        var bldgMaint     = C("Building Maintenance & Repair", HierarchyLevel.Functions, facilities, s2);
        var landscape     = C("Landscaping & Grounds Management", HierarchyLevel.Functions, facilities, s2);
        var utilities     = C("Utilities & Energy Management", HierarchyLevel.Functions, facilities, s2);
        var safetySec     = C("Safety & Security Operations", HierarchyLevel.Functions, facilities, s2);

        // ── L2 — HR ──
        var recruitment   = C("Recruitment & Talent Acquisition", HierarchyLevel.Functions, humanRes, s2);
        var payroll       = C("Payroll & Compensation", HierarchyLevel.Functions, humanRes, s2);
        var training      = C("Training & Professional Development", HierarchyLevel.Functions, humanRes, s2);
        var empRelations  = C("Employee Relations & Compliance", HierarchyLevel.Functions, humanRes, s2);

        // ── L2 — Procurement ──
        var vendorMgmt    = C("Vendor Management & Contracts", HierarchyLevel.Functions, procurement, s2);
        var inventory     = C("Inventory & Warehouse", HierarchyLevel.Functions, procurement, s2);
        var purchasing    = C("Purchasing & Sourcing", HierarchyLevel.Functions, procurement, s2);

        // ── L2 — Food & Beverage ──
        var catering      = C("Catering & Dining Operations", HierarchyLevel.Functions, foodBev, s2);
        var kitchen       = C("Kitchen & Menu Management", HierarchyLevel.Functions, foodBev, s2);
        var cafeteria     = C("Cafeteria & Retail Outlets", HierarchyLevel.Functions, foodBev, s2);
        var foodSafety    = C("Food Safety & Hygiene", HierarchyLevel.Functions, foodBev, s2);

        // ── L2 — Software Dev ──
        var webApps       = C("Web Applications Development", HierarchyLevel.Functions, softwareDev, s3);
        var mobileDev     = C("Mobile & Cross-Platform Development", HierarchyLevel.Functions, softwareDev, s3);
        var enterprise    = C("Enterprise Systems & Integration", HierarchyLevel.Functions, softwareDev, s3);
        var qaTesting     = C("QA & Software Testing", HierarchyLevel.Functions, softwareDev, s3);

        // ── L2 — IT Infrastructure ──
        var networkAdmin  = C("Network Administration", HierarchyLevel.Functions, itInfra, s3);
        var serverCloud   = C("Server & Cloud Management", HierarchyLevel.Functions, itInfra, s3);
        var cybersecurity = C("Cybersecurity", HierarchyLevel.Functions, itInfra, s3);

        // ── L2 — Tech Support ──
        var endUser       = C("End-User Support", HierarchyLevel.Functions, techSupport, s3);
        var hardware      = C("Hardware & Peripherals Management", HierarchyLevel.Functions, techSupport, s3);
        var avClassroom   = C("AV & Smart Classroom Support", HierarchyLevel.Functions, techSupport, s3);

        // ── L2 — Finance ──
        var genAcct       = C("General Accounting & Reporting", HierarchyLevel.Functions, financeAcct, s4);
        var budgeting     = C("Budgeting & Financial Planning", HierarchyLevel.Functions, financeAcct, s4);
        var tuition       = C("Tuition & Fee Collection", HierarchyLevel.Functions, financeAcct, s4);
        var treasury      = C("Treasury & Cash Management", HierarchyLevel.Functions, financeAcct, s4);

        // ── L2 — Legal ──
        var contracts     = C("Contracts & Agreements", HierarchyLevel.Functions, legalCompl, s4);
        var regCompl      = C("Regulatory Compliance", HierarchyLevel.Functions, legalCompl, s4);
        var disputes      = C("Dispute Resolution", HierarchyLevel.Functions, legalCompl, s4);

        // ── L2 — Audit ──
        var finAudit      = C("Financial Audit", HierarchyLevel.Functions, internalAudit, s4);
        var opAudit       = C("Operational Audit", HierarchyLevel.Functions, internalAudit, s4);
        var complAudit    = C("Compliance Audit", HierarchyLevel.Functions, internalAudit, s4);

        // ── L2 — Strategic Planning ──
        var instStrategy  = C("Institutional Strategy & KPIs", HierarchyLevel.Functions, stratPlan, s4);
        var perfMonitor   = C("Performance Monitoring & Evaluation", HierarchyLevel.Functions, stratPlan, s4);
        var orgDev        = C("Organizational Development", HierarchyLevel.Functions, stratPlan, s4);

        // ── L2 — Student Affairs ──
        var admissions    = C("Admissions & Registration", HierarchyLevel.Functions, studentAffairs, s5);
        var studentRec    = C("Student Records & Transcripts", HierarchyLevel.Functions, studentAffairs, s5);
        var studentWelfare= C("Student Welfare & Counseling", HierarchyLevel.Functions, studentAffairs, s5);
        var intlStudents  = C("International Students Office", HierarchyLevel.Functions, studentAffairs, s5);

        // ── L2 — Academic Support ──
        var tutoring      = C("Tutoring & Learning Centers", HierarchyLevel.Functions, acadSupport, s5);
        var advising      = C("Academic Advising", HierarchyLevel.Functions, acadSupport, s5);
        var studySkills   = C("Study Skills & Success Programs", HierarchyLevel.Functions, acadSupport, s5);

        // ── L2 — Student Activities ──
        var clubs         = C("Clubs & Organizations", HierarchyLevel.Functions, studentAct, s5);
        var sports        = C("Sports & Recreation", HierarchyLevel.Functions, studentAct, s5);
        var cultural      = C("Cultural Events & Exhibitions", HierarchyLevel.Functions, studentAct, s5);

        // ── L2 — Career Services ──
        var careerCounsel = C("Career Counseling & Placement", HierarchyLevel.Functions, careerServices, s5);
        var internships   = C("Internship & Co-op Programs", HierarchyLevel.Functions, careerServices, s5);
        var alumni        = C("Alumni Network & Fundraising", HierarchyLevel.Functions, careerServices, s5);

        // ══════════════════════════════════════════════════════════
        // L3 PROCESSES (only those with data in test data)
        // ══════════════════════════════════════════════════════════

        // Sector 1 L3
        var courseDesign  = C("Course Design & Syllabus Review", HierarchyLevel.Processes, currDev, s1);
        var progAccred    = C("Program Accreditation Alignment", HierarchyLevel.Processes, currDev, s1);
        var currBench     = C("Curriculum Benchmarking", HierarchyLevel.Processes, currDev, s1);
        var facRecruit    = C("Faculty Recruitment & Onboarding", HierarchyLevel.Processes, teachFaculty, s1);
        var teachEval     = C("Teaching Evaluation & Feedback", HierarchyLevel.Processes, teachFaculty, s1);
        var visitProfs    = C("Visiting Professors & Adjuncts", HierarchyLevel.Processes, teachFaculty, s1);
        var examSched     = C("Exam Scheduling & Logistics", HierarchyLevel.Processes, examAssess, s1);
        var gradStd       = C("Grading Standardization", HierarchyLevel.Processes, examAssess, s1);
        var acadInteg     = C("Academic Integrity & Plagiarism", HierarchyLevel.Processes, examAssess, s1);
        var lmsAdmin      = C("LMS Administration", HierarchyLevel.Processes, eLearning, s1);
        var digContent    = C("Digital Content Production", HierarchyLevel.Processes, eLearning, s1);
        var onlineAssess  = C("Online Assessment Tools", HierarchyLevel.Processes, eLearning, s1);
        var grantApp      = C("Grant Application Support", HierarchyLevel.Processes, resGrants, s1);
        var postAward     = C("Post-Award Administration", HierarchyLevel.Processes, resGrants, s1);
        var fundSource    = C("Funding Source Identification", HierarchyLevel.Processes, resGrants, s1);
        var thesisMgmt    = C("Thesis & Dissertation Management", HierarchyLevel.Processes, gradPrograms, s1);
        var gradAdmiss    = C("Graduate Admissions", HierarchyLevel.Processes, gradPrograms, s1);
        var gradTrack     = C("Graduate Student Progress Tracking", HierarchyLevel.Processes, gradPrograms, s1);
        var qaAudits      = C("QA Audits & Reviews", HierarchyLevel.Processes, qaStandards, s1);
        var stdDocs       = C("Standards Documentation", HierarchyLevel.Processes, qaStandards, s1);
        var contImprove   = C("Continuous Improvement Initiatives", HierarchyLevel.Processes, qaStandards, s1);

        // Sector 2 L3
        var prevMaint     = C("Preventive Maintenance Scheduling", HierarchyLevel.Processes, bldgMaint, s2);
        var emergRepair   = C("Emergency Repair Management", HierarchyLevel.Processes, bldgMaint, s2);
        var contractCoord = C("Contractor Coordination", HierarchyLevel.Processes, bldgMaint, s2);
        var powerHvac     = C("Power & HVAC Monitoring", HierarchyLevel.Processes, utilities, s2);
        var waterWaste    = C("Water & Waste Management", HierarchyLevel.Processes, utilities, s2);
        var energyEff     = C("Energy Efficiency Projects", HierarchyLevel.Processes, utilities, s2);
        var campusPatrol  = C("Campus Patrol & Access Control", HierarchyLevel.Processes, safetySec, s2);
        var incidentResp  = C("Incident Response & Reporting", HierarchyLevel.Processes, safetySec, s2);
        var fireSafety    = C("Fire Safety & Emergency Drills", HierarchyLevel.Processes, safetySec, s2);
        var jobPosting    = C("Job Posting & Sourcing", HierarchyLevel.Processes, recruitment, s2);
        var interview     = C("Interview & Selection", HierarchyLevel.Processes, recruitment, s2);
        var onboarding    = C("Onboarding & Orientation", HierarchyLevel.Processes, recruitment, s2);
        var monthPayroll  = C("Monthly Payroll Processing", HierarchyLevel.Processes, payroll, s2);
        var benefits      = C("Benefits Administration", HierarchyLevel.Processes, payroll, s2);
        var salaryBench   = C("Salary Benchmarking & Adjustments", HierarchyLevel.Processes, payroll, s2);
        var trainNeeds    = C("Training Needs Assessment", HierarchyLevel.Processes, training, s2);
        var workshopDel   = C("Workshop & Program Delivery", HierarchyLevel.Processes, training, s2);
        var certTraining  = C("Certification & Compliance Training", HierarchyLevel.Processes, training, s2);
        var dailyMeal     = C("Daily Meal Production", HierarchyLevel.Processes, catering, s2);
        var eventCater    = C("Event Catering", HierarchyLevel.Processes, catering, s2);
        var dietaryCater  = C("Dietary & Special Needs Catering", HierarchyLevel.Processes, catering, s2);
        var kitchenInsp   = C("Kitchen Inspection & Sanitation", HierarchyLevel.Processes, foodSafety, s2);
        var supplierQA    = C("Supplier Quality Audits", HierarchyLevel.Processes, foodSafety, s2);
        var staffHygiene  = C("Staff Health & Hygiene Training", HierarchyLevel.Processes, foodSafety, s2);

        // Sector 3 L3
        var frontend      = C("Frontend Development", HierarchyLevel.Processes, webApps, s3);
        var backendApi    = C("Backend & API Development", HierarchyLevel.Processes, webApps, s3);
        var dbDesign      = C("Database Design & Optimization", HierarchyLevel.Processes, webApps, s3);
        var iosAndroid    = C("iOS & Android Native Development", HierarchyLevel.Processes, mobileDev, s3);
        var crossPlatform = C("Cross-Platform Frameworks", HierarchyLevel.Processes, mobileDev, s3);
        var mobileQA      = C("Mobile QA & Device Testing", HierarchyLevel.Processes, mobileDev, s3);
        var erpInteg      = C("ERP & SIS Integration", HierarchyLevel.Processes, enterprise, s3);
        var apiGateway    = C("API Gateway & Middleware", HierarchyLevel.Processes, enterprise, s3);
        var dataMigration = C("Data Migration & ETL", HierarchyLevel.Processes, enterprise, s3);
        var testPlan      = C("Test Planning & Strategy", HierarchyLevel.Processes, qaTesting, s3);
        var autoTest      = C("Automated Testing", HierarchyLevel.Processes, qaTesting, s3);
        var perfTest      = C("Performance & Load Testing", HierarchyLevel.Processes, qaTesting, s3);
        var lanWan        = C("LAN/WAN Management", HierarchyLevel.Processes, networkAdmin, s3);
        var vpn           = C("VPN & Remote Access", HierarchyLevel.Processes, networkAdmin, s3);
        var netMonitor    = C("Network Monitoring & Troubleshooting", HierarchyLevel.Processes, networkAdmin, s3);
        var threatDetect  = C("Threat Detection & Response", HierarchyLevel.Processes, cybersecurity, s3);
        var vulnAssess    = C("Vulnerability Assessment & Patching", HierarchyLevel.Processes, cybersecurity, s3);
        var secPolicy     = C("Security Policy & Awareness", HierarchyLevel.Processes, cybersecurity, s3);
        var ticketTriage  = C("Ticket Triage & Resolution", HierarchyLevel.Processes, endUser, s3);
        var selfService   = C("Self-Service Portal Management", HierarchyLevel.Processes, endUser, s3);
        var kbMaint       = C("Knowledge Base Maintenance", HierarchyLevel.Processes, endUser, s3);

        // Sector 4 L3
        var acctPayRec    = C("Accounts Payable / Receivable", HierarchyLevel.Processes, genAcct, s4);
        var finStatements = C("Financial Statement Preparation", HierarchyLevel.Processes, genAcct, s4);
        var taxCompl      = C("Tax Compliance & Filing", HierarchyLevel.Processes, genAcct, s4);
        var annBudget     = C("Annual Budget Preparation", HierarchyLevel.Processes, budgeting, s4);
        var varAnalysis   = C("Variance Analysis & Forecasting", HierarchyLevel.Processes, budgeting, s4);
        var capEx         = C("Capital Expenditure Planning", HierarchyLevel.Processes, budgeting, s4);
        var revAudit      = C("Revenue Cycle Audit", HierarchyLevel.Processes, finAudit, s4);
        var procAudit     = C("Procurement Audit", HierarchyLevel.Processes, finAudit, s4);
        var fixedAssets   = C("Fixed Assets Audit", HierarchyLevel.Processes, finAudit, s4);
        var kpiFramework  = C("KPI Framework Development", HierarchyLevel.Processes, instStrategy, s4);
        var quartReview   = C("Quarterly Performance Reviews", HierarchyLevel.Processes, instStrategy, s4);
        var benchBest     = C("Benchmarking & Best Practices", HierarchyLevel.Processes, instStrategy, s4);

        // Sector 5 L3
        var appProcess    = C("Application Processing", HierarchyLevel.Processes, admissions, s5);
        var courseReg     = C("Course Registration & Scheduling", HierarchyLevel.Processes, admissions, s5);
        var transferCred  = C("Transfer & Credit Evaluation", HierarchyLevel.Processes, admissions, s5);
        var mentalHealth  = C("Mental Health & Counseling Services", HierarchyLevel.Processes, studentWelfare, s5);
        var finAid        = C("Financial Aid & Scholarships", HierarchyLevel.Processes, studentWelfare, s5);
        var grievance     = C("Student Grievance Resolution", HierarchyLevel.Processes, studentWelfare, s5);
        var clubReg       = C("Club Registration & Governance", HierarchyLevel.Processes, clubs, s5);
        var eventPlan     = C("Event Planning & Approvals", HierarchyLevel.Processes, clubs, s5);
        var clubBudget    = C("Budget Allocation for Clubs", HierarchyLevel.Processes, clubs, s5);
        var resumePrep    = C("Resume & Interview Preparation", HierarchyLevel.Processes, careerCounsel, s5);
        var employerRel   = C("Employer Relations & Job Fairs", HierarchyLevel.Processes, careerCounsel, s5);
        var placementTrack= C("Placement Tracking & Analytics", HierarchyLevel.Processes, careerCounsel, s5);

        await context.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════
        // MEMBERSHIPS
        // ══════════════════════════════════════════════════════════

        var memberships = new List<CommitteeMembership>();
        void H(User u, Committee c) => memberships.Add(new CommitteeMembership { User = u, Committee = c, Role = CommitteeRole.Head, EffectiveFrom = now });
        void M(User u, Committee c) => memberships.Add(new CommitteeMembership { User = u, Committee = c, Role = CommitteeRole.Member, EffectiveFrom = now });

        // L0
        H(amira, topLevel); H(khaled, topLevel); H(mariam, topLevel); H(adel, topLevel); H(heba, topLevel);

        // L1 — Sector 1
        H(faridZaki, acadPrograms); M(monaHelal, acadPrograms);
        H(aymanTawfik, researchGrad); M(samiraLotfi, researchGrad);
        H(nohaMahmoud, acadQuality); M(waelKassem, acadQuality);
        H(ranaShaheen, libraryRes);

        // L1 — Sector 2
        H(mahmoudGabr, facilities); M(ashrafYoussef, facilities);
        H(faridaAmin, humanRes); M(ahmedTaha, humanRes);
        H(sherifNaguib, procurement);
        H(waleedHamdi, foodBev); M(hanaMorsi, foodBev);

        // L1 — Sector 3
        H(ibrahimHassan, softwareDev); M(yasminFarouk, softwareDev);
        H(bassemGad, itInfra); M(karimTawfik, itInfra);
        H(hazemRizk, techSupport); M(mahaSamir, techSupport);

        // L1 — Sector 4
        H(heshamFarag, financeAcct); M(nermeenKhalil, financeAcct);
        H(gamalReda, legalCompl);
        H(mostafaSalem, internalAudit); M(emanLotfy, internalAudit);
        H(sawsanOsman, stratPlan);

        // L1 — Sector 5
        H(amalFathi, studentAffairs); M(tamerElSisi, studentAffairs);
        H(halaShafik, acadSupport);
        H(karimAbdallah, studentAct); M(daliaNour, studentAct);
        H(ghadaMohsen, careerServices);

        // ── L2 Memberships — Sector 1 ──
        H(monaHelal, currDev); M(lamiaRefaat, currDev); M(alaaBadawi, currDev); M(wissamKhoury, currDev);
        H(yasserNasr, teachFaculty); M(shahiraTalaat, teachFaculty); M(reemAtef, teachFaculty); M(nabilHamed, teachFaculty);
        H(soniaGuirguis, examAssess); M(hatemBarsoum, examAssess); M(nevineSaad, examAssess); M(rafikHabib, examAssess);
        H(alaaBadawi, eLearning); M(magedBotros, eLearning); M(hodaLabib, eLearning); M(peterAziz, eLearning);
        H(samiraLotfi, resGrants); M(nabilaFikry, resGrants); M(ihabSerag, resGrants); M(yaraMagdy, resGrants);
        H(walidDarwish, gradPrograms); M(asmaaRagab, gradPrograms); M(dinaShaker, gradPrograms); M(bassemKhalaf, gradPrograms);
        H(nabilaFikry, ipPub); M(heshamAnwar, ipPub); M(sallyGerges, ipPub); M(ramySalib, ipPub);
        H(waelKassem, qaStandards); M(manalRizk, qaStandards); M(saharFouad, qaStandards); M(kareemMourad, qaStandards);
        H(manalRizk, instAccred); M(ossamaFathy, instAccred); M(mervatKamal, instAccred); M(amrHennawy, instAccred);
        H(ossamaFathy, acadReviews); M(abeerNazif, acadReviews); M(nagwaAttia, acadReviews); M(ashrafHanna, acadReviews);
        H(ranaShaheen, collMgmt); M(lamisHalim, collMgmt); M(amirFarag, collMgmt); M(suzyBeshay, collMgmt);
        H(amirFarag, digLibrary); M(minaGuirguis, digLibrary); M(hebaSamir, digLibrary); M(georgeYoussef, digLibrary);
        H(lamisHalim, readingHalls); M(bassemNakhla, readingHalls); M(christineAdel, readingHalls);

        // ── L2 Memberships — Sector 2 ──
        H(ashrafYoussef, bldgMaint); M(saadHashem, bldgMaint); M(fadiMilad, bldgMaint); M(nabilGuindi, bldgMaint);
        H(saadHashem, landscape); M(magdyBoulos, landscape); M(nawalSami, landscape); M(rafatHanna, landscape);
        H(haniTawfik, utilities); M(mohsenLabib, utilities); M(sherifWaguih, utilities); M(mayRaouf, utilities);
        H(redaMostafa, safetySec); M(samehGirgis, safetySec); M(emadBarsoum, safetySec); M(waelYoussef, safetySec);
        H(ahmedTaha, recruitment); M(manarEssam, recruitment); M(saraMourad, recruitment); M(hossamAdly, recruitment);
        H(nohaElKady, payroll); M(amgadIshak, payroll); M(vivianLabib, payroll); M(bassemThabet, payroll);
        H(marwaSelim, training); M(ehabMikhail, training); M(nerminAziz, training); M(fadyHabib, training);
        H(hossamAdly, empRelations); M(hendBadr, empRelations); M(wagdyFarid, empRelations); M(rashaGuirguis, empRelations);
        H(sherifNaguib, vendorMgmt); M(aminaLotfy, vendorMgmt); M(ehabSedky, vendorMgmt); M(nadiaFouad, vendorMgmt);
        H(ehabSedky, inventory); M(magdyAbdallah, inventory); M(salwaRizkalla, inventory); M(aymanHanna, inventory);
        H(aminaLotfy, purchasing); M(tarekGhattas, purchasing); M(hodaMalak, purchasing); M(gergesFahmy, purchasing);
        H(waleedHamdi, catering); M(ramezHabib, catering); M(yvonneBoulos, catering); M(shadyMounir, catering);
        H(hanaMorsi, kitchen); M(ramiSaleh, kitchen); M(jacklineSalib, kitchen); M(waelHabashi, kitchen);
        H(ramezHabib, cafeteria); M(mariamGuindi, cafeteria); M(peterNassif, cafeteria); M(dinaBasta, cafeteria);
        H(yvonneBoulos, foodSafety); M(fadyMikhail, foodSafety); M(ireneHabib, foodSafety); M(samehNazmi, foodSafety);

        // ── L2 Memberships — Sector 3 ──
        H(yasminFarouk, webApps); M(tarekBotros, webApps); M(sandraNassif, webApps); M(bahaaSamy, webApps);
        H(tarekBotros, mobileDev); M(marinaSedky, mobileDev); M(sherifBeshay, mobileDev); M(reemMikhail, mobileDev);
        H(ahmedGaber, enterprise); M(monicaHalim, enterprise); M(abanobSobhy, enterprise); M(karimWagdy, enterprise);
        H(sandraNassif, qaTesting); M(johnLabib, qaTesting); M(marianFarid, qaTesting); M(emadYoussef, qaTesting);
        H(karimTawfik, networkAdmin); M(bishoyAziz, networkAdmin); M(hanyFarouk, networkAdmin); M(rafikIshak, networkAdmin);
        H(bishoyAziz, serverCloud); M(markGabra, serverCloud); M(waelSamir, serverCloud); M(nadiaGuirguis, serverCloud);
        H(markGabra, cybersecurity); M(seifHaroun, cybersecurity); M(mirnaLabib, cybersecurity); M(andrewNassif, cybersecurity);
        H(mahaSamir, endUser); M(michaelAdel, endUser); M(randaThabet, endUser); M(rafikFarag, endUser);
        H(michaelAdel, hardware); M(ehabGuirguis, hardware); M(nancySalib, hardware); M(aymanHabib, hardware);
        H(rafikFarag, avClassroom); M(georgeHabib, avClassroom); M(carolineNaguib, avClassroom); M(naderIshak, avClassroom);

        // ── L2 Memberships — Sector 4 ──
        H(nermeenKhalil, genAcct); M(wagdyLabib, genAcct); M(vivianSamir, genAcct); M(ehabNakhla, genAcct);
        H(wagdyLabib, budgeting); M(nagwaSedky, budgeting); M(amrTawfik, budgeting); M(hendFarouk, budgeting);
        H(nagwaSedky, tuition); M(ashrafHabib, tuition); M(sallyGuirguis, tuition); M(magedAziz, tuition);
        H(amrTawfik, treasury); M(lamiaFarag, treasury); M(ramyMounir, treasury);
        H(gamalReda, contracts); M(hodaShaker, contracts); M(bassemNaguib, contracts);
        H(hodaShaker, regCompl); M(amalHabib, regCompl); M(sherifLabib, regCompl); M(raniaGuindi, regCompl);
        H(mohamedBadr, disputes); M(saraYoussef, disputes); M(naderMikhail, disputes);
        H(emanLotfy, finAudit); M(magdyAziz, finAudit); M(lamisFarid, finAudit); M(ihabLabib, finAudit);
        H(magdyAziz, opAudit); M(shereenHabib, opAudit); M(waelNaguib, opAudit); M(mervatSalib, opAudit);
        H(shereenHabib, complAudit); M(amirSedky, complAudit); M(nerminHanna, complAudit);
        H(sawsanOsman, instStrategy); M(amrNazif, instStrategy); M(reemFarag, instStrategy); M(hazemSalib, instStrategy);
        H(amrNazif, perfMonitor); M(shahiraLabib, perfMonitor); M(karimHabib, perfMonitor);
        H(shahiraLabib, orgDev); M(bassemFarid, orgDev); M(dinaIshak, orgDev); M(waelSedky, orgDev);

        // ── L2 Memberships — Sector 5 ──
        H(tamerElSisi, admissions); M(azzaNaguib, admissions); M(hatemFarid, admissions); M(randaSalib, admissions);
        H(azzaNaguib, studentRec); M(aymanLabib, studentRec); M(hodaHabib, studentRec); M(sherifSamir, studentRec);
        H(niveenAdel, studentWelfare); M(hendSalib, studentWelfare); M(fadyNaguib, studentWelfare); M(mariamHabib, studentWelfare);
        H(dinaFarouk, intlStudents); M(andrewLabib, intlStudents); M(mahaIshak, intlStudents);
        H(halaShafik, tutoring); M(bassemGuirguis, tutoring); M(reemSalib, tutoring); M(nancyHabib, tutoring);
        H(ranaAdel, advising); M(saharNaguib, advising); M(emadLabib, advising); M(mirnaFarid, advising);
        H(saharNaguib, studySkills); M(kareemSalib, studySkills); M(hodaNaguib, studySkills);
        H(daliaNour, clubs); M(omarLabib, clubs); M(yaraHabib, clubs); M(ramySalib, clubs);
        H(omarLabib, sports); M(alaaFarid, sports); M(nadiaHabib, sports); M(waelGuirguis, sports);
        H(yaraHabib, cultural); M(bassemSamir, cultural); M(lamisNaguib, cultural); M(ranaHabib, cultural);
        H(ghadaMohsen, careerCounsel); M(reemLabib, careerCounsel); M(hossamFarid, careerCounsel); M(sallyHabib, careerCounsel);
        H(reemLabib, internships); M(tamerGuirguis, internships); M(hodaFarid, internships); M(amrSalib, internships);
        H(hossamFarid, alumni); M(mahaLabib, alumni); M(georgeNaguib, alumni); M(dinaHabib, alumni);

        // ── L3 Memberships (abbreviated — leads + members) ──
        // Sector 1 L3
        H(lamiaRefaat, courseDesign); M(alaaBadawi, courseDesign); M(wissamKhoury, courseDesign);
        H(alaaBadawi, progAccred); M(lamiaRefaat, progAccred); M(reemAtef, progAccred);
        H(wissamKhoury, currBench); M(monaHelal, currBench); M(lamiaRefaat, currBench);
        H(shahiraTalaat, facRecruit); M(reemAtef, facRecruit); M(nabilHamed, facRecruit);
        H(nabilHamed, teachEval); M(yasserNasr, teachEval); M(shahiraTalaat, teachEval);
        H(reemAtef, visitProfs); M(shahiraTalaat, visitProfs); M(yasserNasr, visitProfs);
        H(hatemBarsoum, examSched); M(nevineSaad, examSched); M(rafikHabib, examSched);
        H(nevineSaad, gradStd); M(soniaGuirguis, gradStd); M(hatemBarsoum, gradStd);
        H(rafikHabib, acadInteg); M(soniaGuirguis, acadInteg); M(nevineSaad, acadInteg);
        H(magedBotros, lmsAdmin); M(hodaLabib, lmsAdmin); M(peterAziz, lmsAdmin);
        H(hodaLabib, digContent); M(magedBotros, digContent); M(alaaBadawi, digContent);
        H(peterAziz, onlineAssess); M(magedBotros, onlineAssess); M(hodaLabib, onlineAssess);
        H(nabilaFikry, grantApp); M(ihabSerag, grantApp); M(yaraMagdy, grantApp);
        H(ihabSerag, postAward); M(samiraLotfi, postAward); M(yaraMagdy, postAward);
        H(yaraMagdy, fundSource); M(nabilaFikry, fundSource); M(samiraLotfi, fundSource);
        H(asmaaRagab, thesisMgmt); M(dinaShaker, thesisMgmt); M(bassemKhalaf, thesisMgmt);
        H(dinaShaker, gradAdmiss); M(walidDarwish, gradAdmiss); M(asmaaRagab, gradAdmiss);
        H(bassemKhalaf, gradTrack); M(walidDarwish, gradTrack); M(dinaShaker, gradTrack);
        H(manalRizk, qaAudits); M(saharFouad, qaAudits); M(kareemMourad, qaAudits);
        H(saharFouad, stdDocs); M(waelKassem, stdDocs); M(kareemMourad, stdDocs);
        H(kareemMourad, contImprove); M(manalRizk, contImprove); M(saharFouad, contImprove);

        // Sector 2 L3
        H(saadHashem, prevMaint); M(fadiMilad, prevMaint); M(nabilGuindi, prevMaint);
        H(fadiMilad, emergRepair); M(ashrafYoussef, emergRepair); M(nabilGuindi, emergRepair);
        H(nabilGuindi, contractCoord); M(saadHashem, contractCoord); M(ashrafYoussef, contractCoord);
        H(mohsenLabib, powerHvac); M(sherifWaguih, powerHvac); M(mayRaouf, powerHvac);
        H(sherifWaguih, waterWaste); M(haniTawfik, waterWaste); M(mayRaouf, waterWaste);
        H(mayRaouf, energyEff); M(mohsenLabib, energyEff); M(haniTawfik, energyEff);
        H(samehGirgis, campusPatrol); M(emadBarsoum, campusPatrol); M(waelYoussef, campusPatrol);
        H(emadBarsoum, incidentResp); M(redaMostafa, incidentResp); M(samehGirgis, incidentResp);
        H(waelYoussef, fireSafety); M(redaMostafa, fireSafety); M(samehGirgis, fireSafety);
        H(manarEssam, jobPosting); M(saraMourad, jobPosting); M(hossamAdly, jobPosting);
        H(saraMourad, interview); M(ahmedTaha, interview); M(manarEssam, interview);
        H(hossamAdly, onboarding); M(manarEssam, onboarding); M(marwaSelim, onboarding);
        H(amgadIshak, monthPayroll); M(vivianLabib, monthPayroll); M(bassemThabet, monthPayroll);
        H(vivianLabib, benefits); M(nohaElKady, benefits); M(amgadIshak, benefits);
        H(bassemThabet, salaryBench); M(nohaElKady, salaryBench); M(vivianLabib, salaryBench);
        H(ehabMikhail, trainNeeds); M(nerminAziz, trainNeeds); M(fadyHabib, trainNeeds);
        H(nerminAziz, workshopDel); M(marwaSelim, workshopDel); M(ehabMikhail, workshopDel);
        H(fadyHabib, certTraining); M(marwaSelim, certTraining); M(nerminAziz, certTraining);
        H(ramezHabib, dailyMeal); M(yvonneBoulos, dailyMeal); M(shadyMounir, dailyMeal);
        H(yvonneBoulos, eventCater); M(waleedHamdi, eventCater); M(ramezHabib, eventCater);
        H(shadyMounir, dietaryCater); M(hanaMorsi, dietaryCater); M(ramezHabib, dietaryCater);
        H(fadyMikhail, kitchenInsp); M(ireneHabib, kitchenInsp); M(samehNazmi, kitchenInsp);
        H(ireneHabib, supplierQA); M(yvonneBoulos, supplierQA); M(fadyMikhail, supplierQA);
        H(samehNazmi, staffHygiene); M(fadyMikhail, staffHygiene); M(ireneHabib, staffHygiene);

        // Sector 3 L3
        H(sandraNassif, frontend); M(bahaaSamy, frontend); M(tarekBotros, frontend);
        H(tarekBotros, backendApi); M(yasminFarouk, backendApi); M(ahmedGaber, backendApi);
        H(bahaaSamy, dbDesign); M(monicaHalim, dbDesign); M(tarekBotros, dbDesign);
        H(marinaSedky, iosAndroid); M(sherifBeshay, iosAndroid); M(reemMikhail, iosAndroid);
        H(sherifBeshay, crossPlatform); M(tarekBotros, crossPlatform); M(marinaSedky, crossPlatform);
        H(reemMikhail, mobileQA); M(sandraNassif, mobileQA); M(sherifBeshay, mobileQA);
        H(monicaHalim, erpInteg); M(abanobSobhy, erpInteg); M(karimWagdy, erpInteg);
        H(karimWagdy, apiGateway); M(ahmedGaber, apiGateway); M(monicaHalim, apiGateway);
        H(abanobSobhy, dataMigration); M(monicaHalim, dataMigration); M(karimWagdy, dataMigration);
        H(johnLabib, testPlan); M(marianFarid, testPlan); M(emadYoussef, testPlan);
        H(marianFarid, autoTest); M(sandraNassif, autoTest); M(johnLabib, autoTest);
        H(emadYoussef, perfTest); M(johnLabib, perfTest); M(marianFarid, perfTest);
        H(bishoyAziz, lanWan); M(hanyFarouk, lanWan); M(rafikIshak, lanWan);
        H(hanyFarouk, vpn); M(karimTawfik, vpn); M(rafikIshak, vpn);
        H(rafikIshak, netMonitor); M(bishoyAziz, netMonitor); M(hanyFarouk, netMonitor);
        H(seifHaroun, threatDetect); M(mirnaLabib, threatDetect); M(andrewNassif, threatDetect);
        H(mirnaLabib, vulnAssess); M(markGabra, vulnAssess); M(seifHaroun, vulnAssess);
        H(andrewNassif, secPolicy); M(seifHaroun, secPolicy); M(mirnaLabib, secPolicy);
        H(michaelAdel, ticketTriage); M(randaThabet, ticketTriage); M(rafikFarag, ticketTriage);
        H(randaThabet, selfService); M(mahaSamir, selfService); M(michaelAdel, selfService);
        H(rafikFarag, kbMaint); M(randaThabet, kbMaint); M(michaelAdel, kbMaint);

        // Sector 4 L3
        H(wagdyLabib, acctPayRec); M(vivianSamir, acctPayRec); M(ehabNakhla, acctPayRec);
        H(vivianSamir, finStatements); M(nermeenKhalil, finStatements); M(wagdyLabib, finStatements);
        H(ehabNakhla, taxCompl); M(wagdyLabib, taxCompl); M(vivianSamir, taxCompl);
        H(nagwaSedky, annBudget); M(amrTawfik, annBudget); M(hendFarouk, annBudget);
        H(amrTawfik, varAnalysis); M(wagdyLabib, varAnalysis); M(nagwaSedky, varAnalysis);
        H(hendFarouk, capEx); M(nagwaSedky, capEx); M(amrTawfik, capEx);
        H(magdyAziz, revAudit); M(lamisFarid, revAudit); M(ihabLabib, revAudit);
        H(lamisFarid, procAudit); M(emanLotfy, procAudit); M(magdyAziz, procAudit);
        H(ihabLabib, fixedAssets); M(magdyAziz, fixedAssets); M(lamisFarid, fixedAssets);
        H(amrNazif, kpiFramework); M(reemFarag, kpiFramework); M(hazemSalib, kpiFramework);
        H(shahiraLabib, quartReview); M(sawsanOsman, quartReview); M(amrNazif, quartReview);
        H(hazemSalib, benchBest); M(shahiraLabib, benchBest); M(bassemFarid, benchBest);

        // Sector 5 L3
        H(azzaNaguib, appProcess); M(hatemFarid, appProcess); M(randaSalib, appProcess);
        H(hatemFarid, courseReg); M(tamerElSisi, courseReg); M(azzaNaguib, courseReg);
        H(randaSalib, transferCred); M(azzaNaguib, transferCred); M(hatemFarid, transferCred);
        H(niveenAdel, mentalHealth); M(hendSalib, mentalHealth); M(fadyNaguib, mentalHealth);
        H(fadyNaguib, finAid); M(niveenAdel, finAid); M(mariamHabib, finAid);
        H(hendSalib, grievance); M(niveenAdel, grievance); M(fadyNaguib, grievance);
        H(omarLabib, clubReg); M(yaraHabib, clubReg); M(ramySalib, clubReg);
        H(yaraHabib, eventPlan); M(daliaNour, eventPlan); M(omarLabib, eventPlan);
        H(ramySalib, clubBudget); M(omarLabib, clubBudget); M(daliaNour, clubBudget);
        H(reemLabib, resumePrep); M(hossamFarid, resumePrep); M(sallyHabib, resumePrep);
        H(hossamFarid, employerRel); M(ghadaMohsen, employerRel); M(reemLabib, employerRel);
        H(sallyHabib, placementTrack); M(ghadaMohsen, placementTrack); M(hossamFarid, placementTrack);

        context.CommitteeMemberships.AddRange(memberships);
        await context.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════
        // SHADOW ASSIGNMENTS
        // ══════════════════════════════════════════════════════════

        var shadows = new List<ShadowAssignment>
        {
            new() { PrincipalUser = amira,  ShadowUser = shAmira,  Committee = topLevel, IsActive = true, EffectiveFrom = now },
            new() { PrincipalUser = khaled, ShadowUser = shKhaled, Committee = topLevel, IsActive = true, EffectiveFrom = now },
            new() { PrincipalUser = mariam, ShadowUser = shMariam, Committee = topLevel, IsActive = true, EffectiveFrom = now },
            new() { PrincipalUser = adel,   ShadowUser = shAdel,   Committee = topLevel, IsActive = true, EffectiveFrom = now },
            new() { PrincipalUser = heba,   ShadowUser = shHeba,   Committee = topLevel, IsActive = true, EffectiveFrom = now },
        };

        context.ShadowAssignments.AddRange(shadows);
        await context.SaveChangesAsync();
    }
}
