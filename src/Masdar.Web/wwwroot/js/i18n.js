(() => {
    "use strict";

    const storageKey = "masdar-language";
    const root = document.documentElement;
    const originalText = new WeakMap();
    const originalAttributes = new WeakMap();

    const strings = {
        en: {
            skipContent: "Skip to main content",
            workspace: "Workspace",
            home: "Home",
            allCourses: "All courses",
            courses: "Courses",
            exploreResources: "Explore resources",
            explore: "Explore",
            contribute: "Contribute",
            upload: "Upload",
            community: "Community",
            currentCourse: "Current course",
            switchCourse: "Switch",
            openCourseWorkspace: "Open course materials and roadmap",
            courseTutor: "Course-aware AI tutor",
            masdarTutor: "Masdar AI tutor",
            tutorGrounded: "Ask questions grounded in your selected course resources.",
            askTutor: "Ask the tutor",
            globalSearch: "Search a topic, course, or resource",
            knowledgeHub: "Your university knowledge hub",
            privacy: "Privacy"
        },
        ar: {
            skipContent: "انتقل إلى المحتوى الرئيسي",
            workspace: "مساحة العمل",
            home: "الرئيسية",
            allCourses: "جميع المساقات",
            courses: "المساقات",
            exploreResources: "استكشف المصادر",
            explore: "استكشف",
            contribute: "ساهم",
            upload: "ارفع ملفاً",
            community: "المجتمع",
            currentCourse: "المساق الحالي",
            switchCourse: "تغيير",
            openCourseWorkspace: "افتح مواد المساق وخطة التعلّم",
            courseTutor: "مدرّس مصدر الذكي",
            masdarTutor: "مدرّس مصدر الذكي",
            tutorGrounded: "اسأل أسئلة تستند إلى مصادر مساقك المحدد.",
            askTutor: "اسأل المدرّس",
            globalSearch: "ابحث عن موضوع أو مساق أو مصدر",
            knowledgeHub: "مركز المعرفة الجامعية الخاص بك",
            privacy: "الخصوصية"
        }
    };

    const arabicText = new Map(Object.entries({
        "Welcome back": "مرحباً بعودتك",
        "Find the right material. Learn with confidence.": "اعثر على المادة المناسبة وتعلّم بثقة.",
        "Course catalog": "دليل المساقات",
        "Find course resources": "ابحث عن مصادر المساقات",
        "Start with your academic context, then compare resources using clear community quality signals.": "ابدأ بسياقك الأكاديمي، ثم قارن المصادر باستخدام مؤشرات جودة واضحة من المجتمع.",
        "Share a resource": "شارك مصدراً",
        "What are you studying?": "ماذا تدرس؟",
        "Select one or more courses": "اختر مساقاً واحداً أو أكثر",
        "University": "الجامعة",
        "Major": "التخصص",
        "Selected courses": "المساقات المحددة",
        "All resources": "كل المصادر",
        "Previous exams": "امتحانات سابقة",
        "Assignments": "واجبات",
        "Quiz banks": "بنوك أسئلة",
        "Lecture summaries": "ملخصات المحاضرات",
        "Cheat sheets": "أوراق مراجعة",
        "Student notes": "ملاحظات الطلبة",
        "Videos": "فيديوهات",
        "External links": "روابط خارجية",
        "Saved": "المحفوظة",
        "Highest quality": "الأعلى جودة",
        "Most helpful": "الأكثر فائدة",
        "Most recent": "الأحدث",
        "Newest": "الأحدث",
        "Search": "بحث",
        "Filter": "تصفية",
        "Clear filters": "مسح التصفية",
        "Download": "تنزيل",
        "Preview": "معاينة",
        "View details": "عرض التفاصيل",
        "Save": "حفظ",
        "Saved resources": "المصادر المحفوظة",
        "Helpful": "مفيد",
        "Rate this resource": "قيّم هذا المصدر",
        "Community verified": "موثّق من المجتمع",
        "In review": "قيد المراجعة",
        "Overview": "نظرة عامة",
        "Materials": "المواد",
        "Roadmap": "خطة التعلّم",
        "Career relevance": "الارتباط المهني",
        "Ask course tutor": "اسأل مدرّس المساق",
        "Browse materials": "تصفّح المواد",
        "Focus for your next exam": "ركّز لامتحانك القادم",
        "Top resources for this course": "أفضل مصادر هذا المساق",
        "Smart course roadmap": "خطة مساق ذكية",
        "Why this course matters": "لماذا هذا المساق مهم؟",
        "Opportunities for students": "فرص للطلبة",
        "Relevant opportunities": "الفرص ذات الصلة",
        "Register interest": "سجّل اهتمامك",
        "Registered": "مسجّل",
        "Mark complete": "وضع علامة مكتمل",
        "Mark incomplete": "وضع علامة غير مكتمل",
        "Community contribution": "مساهمة مجتمعية",
        "Share a study resource": "شارك مصدراً دراسياً",
        "Choose your file": "اختر ملفك",
        "Resource title": "عنوان المصدر",
        "Resource type": "نوع المصدر",
        "Topic tags": "وسوم الموضوع",
        "Continue": "متابعة",
        "Back": "رجوع",
        "Review": "مراجعة",
        "Publish resource": "نشر المصدر",
        "Students help students": "الطلبة يساعدون بعضهم",
        "Resource requests": "طلبات المصادر",
        "Top contributors": "أفضل المساهمين",
        "New request": "طلب جديد",
        "Fulfill request": "تلبية الطلب",
        "Post request": "نشر الطلب",
        "Cancel": "إلغاء",
        "My profile": "ملفي الشخصي",
        "Account settings": "إعدادات الحساب",
        "Administration": "الإدارة",
        "Sign out": "تسجيل الخروج",
        "Sign in": "تسجيل الدخول",
        "Create account": "إنشاء حساب",
        "Email": "البريد الإلكتروني",
        "Password": "كلمة المرور",
        "Remember me": "تذكّرني",
        "Admin dashboard": "لوحة تحكم الإدارة",
        "Overview": "نظرة عامة",
        "Users": "المستخدمون",
        "Universities": "الجامعات",
        "Majors": "التخصصات",
        "Resources": "المصادر",
        "Opportunities": "الفرص",
        "Audit log": "سجل التدقيق",
        "Manage all": "إدارة الكل",
        "Recent resources": "أحدث المصادر",
        "Recent activity": "أحدث النشاطات",
        "Active": "نشط",
        "Inactive": "غير نشط",
        "Published": "منشور",
        "Pending review": "بانتظار المراجعة",
        "Course": "المساق",
        "Status": "الحالة",
        "Actions": "الإجراءات",
        "Edit": "تعديل",
        "Details": "التفاصيل",
        "Add university": "إضافة جامعة",
        "Add major": "إضافة تخصص",
        "Add course": "إضافة مساق",
        "Add opportunity": "إضافة فرصة",
        "Save changes": "حفظ التغييرات",
        "Masdar AI tutor": "مدرّس مصدر الذكي",
        "Your active course": "مساقك الحالي",
        "Ask about this course": "اسأل عن هذا المساق",
        "Send": "إرسال",
        "Explain this simply": "اشرح هذا ببساطة",
        "Give me an example": "أعطني مثالاً",
        "Create a quiz": "أنشئ اختباراً",
        "Build a study plan": "أنشئ خطة دراسة",
        "Sources used": "المصادر المستخدمة",
        "No matching resources": "لا توجد مصادر مطابقة",
        "No data available yet.": "لا توجد بيانات بعد.",
        "Previous": "السابق",
        "Next": "التالي",
        "Page not found": "الصفحة غير موجودة",
        "Access denied": "تم رفض الوصول",
        "Return home": "العودة للرئيسية",

        "Browse": "تصفّح",
        "Find": "اعثر",
        "Learn": "تعلّم",
        "Choose courses to add": "اختر مساقات لإضافتها",
        "No active courses in this major.": "لا توجد مساقات نشطة في هذا التخصص.",
        "Apply course selection": "تطبيق اختيار المساقات",
        "Update context": "تحديث السياق",
        "All semesters": "جميع الفصول",
        "Saved only": "المحفوظة فقط",
        "No saved resources here": "لا توجد مصادر محفوظة هنا",
        "Save useful material and it will appear in this view.": "احفظ المواد المفيدة لتظهر في هذه الصفحة.",
        "Try another keyword or category. You can also request what you need from the community.": "جرّب كلمة مفتاحية أو فئة أخرى. ويمكنك أيضاً طلب ما تحتاجه من المجتمع.",
        "Show all resources": "عرض كل المصادر",
        "Request a resource": "اطلب مصدراً",
        "Verified": "موثّق",
        "Rep.": "نموذجي",
        "New": "جديد",
        "View resource": "عرض المصدر",
        "Resource details": "تفاصيل المصدر",
        "Representative of course style": "يمثّل نمط المساق",
        "Download original": "تنزيل الملف الأصلي",
        "Resource preview": "معاينة المصدر",
        "What this material covers": "ما الذي تغطيه هذه المادة",
        "A written preview was not supplied for this resource. Download the original file to review its complete contents.": "لم تُرفق معاينة مكتوبة لهذا المصدر. نزّل الملف الأصلي للاطلاع على محتواه كاملاً.",
        "Course-aware context": "سياق مرتبط بالمساق",
        "Ready for grounded study help": "جاهز لمساعدة دراسية مستندة إلى المصادر",
        "Sign in to ask": "سجّل الدخول للسؤال",
        "Original file": "الملف الأصلي",
        "Community quality": "الجودة بحسب تقييم المجتمع",
        "Helpful votes": "أصوات الإفادة",
        "Representative": "نموذجي",
        "Yes": "نعم",
        "Reviewing": "قيد المراجعة",
        "Trust status": "حالة التوثيق",
        "Was this useful?": "هل كان هذا مفيداً؟",
        "♡ Helpful ✓": "♡ مفيد ✓",
        "♡ Mark helpful": "♡ وضع علامة مفيد",
        "◇ Saved ✓": "◇ محفوظ ✓",
        "◇ Save": "◇ حفظ",
        "Improve this resource": "ساهم في تحسين هذا المصدر",
        "Sign in to rate, save, and mark this material as helpful.": "سجّل الدخول لتقييم هذه المادة وحفظها ووضع علامة مفيدة عليها.",
        "Ask about this resource": "اسأل عن هذا المصدر",
        "Uses this course as tutor context": "يستخدم هذا المساق سياقاً للمدرّس",
        "Sign in to ask the tutor": "سجّل الدخول لسؤال المدرّس",

        "Find your course": "اعثر على مساقك",
        "Choose your academic context or search by course code, title, or instructor.": "اختر سياقك الأكاديمي أو ابحث برمز المساق أو اسمه أو المدرّس.",
        "All universities": "جميع الجامعات",
        "All majors": "جميع التخصصات",
        "Find courses": "البحث عن المساقات",
        "Open a course to see its materials, roadmap, tutor, and opportunities.": "افتح مساقاً للاطلاع على مواده وخطة تعلّمه ومدرّسه وفرصه.",
        "No matching courses": "لا توجد مساقات مطابقة",
        "Try another course code or remove one of the academic filters.": "جرّب رمز مساق آخر أو أزل أحد عوامل التصفية الأكاديمية.",
        "Show all courses": "عرض كل المساقات",
        "Active course": "المساق النشط",
        "trusted materials": "مواد موثوقة",
        "roadmap": "خطة التعلّم",
        "Open course workspace": "فتح مساحة عمل المساق",
        "Make active & ask tutor": "اجعله نشطاً واسأل المدرّس",
        "Make active": "اجعله المساق النشط",
        "complete": "مكتمل",
        "Compare quality before you spend time reading.": "قارن الجودة قبل أن تستثمر وقتك في القراءة.",
        "Search & filter": "بحث وتصفية",
        "Add resource": "إضافة مصدر",
        "No resources yet": "لا توجد مصادر بعد",
        "Be the first to contribute a useful PDF or DOCX to this course.": "كن أول من يشارك ملف PDF أو DOCX مفيداً لهذا المساق.",
        "Upload resource": "رفع مصدر",
        "Preview resource": "معاينة المصدر",
        "A guided path shaped by difficulty and exam importance. Your progress stays private.": "مسار موجّه يرتّب المحتوى بحسب الصعوبة وأهميته للامتحان. يبقى تقدّمك خاصاً.",
        "Roadmap coming soon": "خطة التعلّم ستتوفر قريباً",
        "The course path has not been mapped yet.": "لم تُحدّد خطة هذا المساق بعد.",
        "Sign in to track": "سجّل الدخول لتتبّع التقدّم",
        "Connect academic concepts to real systems and career paths.": "اربط المفاهيم الأكاديمية بالأنظمة الواقعية والمسارات المهنية.",
        "Career mapping coming soon": "خريطة المسارات المهنية ستتوفر قريباً",
        "We are connecting this course to practical applications.": "نعمل على ربط هذا المساق بتطبيقات عملية.",
        "Apply what you learn beyond the classroom.": "طبّق ما تتعلّمه خارج قاعة الدراسة.",
        "No upcoming opportunities": "لا توجد فرص قادمة",
        "New workshops, competitions, and internships will appear here.": "ستظهر هنا ورش العمل والمسابقات والتدريبات الجديدة.",
        "✓ Registered · Cancel": "✓ مسجّل · إلغاء",
        "Sign in to register": "سجّل الدخول للتسجيل",
        "Next exam focus": "محور الامتحان القادم",
        "Prioritized using exam weight and your roadmap progress.": "مرتّب حسب وزن الموضوع في الامتحان وتقدّمك في خطة التعلّم.",
        "Current roadmap complete": "اكتملت خطة التعلّم الحالية",
        "Review your course resources or revisit any topic.": "راجع مصادر مساقك أو عُد إلى أي موضوع.",
        "Open roadmap": "فتح خطة التعلّم",
        "Ask questions in course context": "اطرح أسئلة ضمن سياق المساق",
        "Make active & start a conversation": "اجعله نشطاً وابدأ محادثة",
        "Start a conversation": "ابدأ محادثة",
        "Community-ranked and verified.": "مرتّب حسب تقييم المجتمع وموثّق.",
        "View all": "عرض الكل",
        "No published resources yet": "لا توجد مصادر منشورة بعد",
        "Share the first useful material for this course.": "شارك أول مادة مفيدة لهذا المساق.",

        "Grounded study help": "مساعدة دراسية مستندة إلى المصادر",
        "Explanations, examples, quizzes, and study plans grounded in your active course resources.": "شروحات وأمثلة واختبارات وخطط دراسة تستند إلى مصادر مساقك النشط.",
        "Choose an active course first": "اختر مساقاً نشطاً أولاً",
        "The tutor never asks you to pick a subject. It automatically follows the course you mark active, which keeps every answer and citation inside the right course.": "لن يطلب منك المدرّس اختيار مادة؛ فهو يتبع تلقائياً المساق الذي تحدده كنشط، لتبقى كل إجابة وإحالة ضمن المساق الصحيح.",
        "Choose from Courses": "اختر من المساقات",
        "Change active course": "تغيير المساق النشط",
        "Course context reset.": "تمت إعادة ضبط سياق المساق.",
        "Your active course changed while this page was open. The previous question was cleared so it cannot be answered from the wrong subject.": "تغيّر مساقك النشط أثناء فتح هذه الصفحة، لذلك مُسح السؤال السابق حتى لا تتم الإجابة عنه من سياق مساق آخر.",
        "You": "أنت",
        "Outside active course": "خارج نطاق المساق النشط",
        "No eligible source was available, so the tutor did not invent a citation.": "لم يتوفر مصدر مؤهل، لذلك لم ينشئ المدرّس إحالة غير حقيقية.",
        "Quick prompts": "طلبات سريعة",
        "Start with a learning goal.": "ابدأ بهدف تعلّم.",
        "Course-grounded by design": "مصمم ليستند إلى المساق",
        "Drafts, rejected uploads, unvalidated files, and resources from other courses are excluded before ranking.": "تُستبعد المسودات والملفات المرفوضة أو غير المتحقق منها ومصادر المساقات الأخرى قبل الترتيب.",
        "Continue learning": "واصل التعلّم",
        "What your course sources say": "ما تقوله مصادر مساقك",

        "Platform intelligence": "مؤشرات المنصة",
        "MASDAR at a glance": "مصدر في لمحة",
        "Moderation priorities, community growth, learning quality, and recent platform activity in one operational view.": "أولويات المراجعة ونمو المجتمع وجودة التعلّم وأحدث نشاطات المنصة في عرض تشغيلي واحد.",
        "Live database": "قاعدة بيانات مباشرة",
        "Refresh": "تحديث",
        "Review resources": "مراجعة المصادر",
        "Add a course": "إضافة مساق",
        "Expand the active catalog": "وسّع دليل المساقات النشطة",
        "Publish opportunity": "نشر فرصة",
        "Share an upcoming program": "شارك برنامجاً قادماً",
        "Open audit log": "فتح سجل التدقيق",
        "Trace administrative changes": "تتبّع التغييرات الإدارية",
        "Community members": "أعضاء المجتمع",
        "Published resources": "المصادر المنشورة",
        "Active registrations": "التسجيلات النشطة",
        "joined": "انضموا",
        "published": "نُشرت",
        "created": "أُنشئت",
        "registered": "سُجّلت",
        "7 days": "7 أيام",
        "30 days": "30 يوماً",
        "Action required": "إجراء مطلوب",
        "Moderation queue": "قائمة انتظار المراجعة",
        "Oldest and highest-risk submissions are shown first.": "تظهر الطلبات الأقدم والأعلى خطورة أولاً.",
        "Queue cleared": "قائمة الانتظار خالية",
        "There are no pending or scan-blocked resources.": "لا توجد مصادر معلّقة أو محظورة بسبب الفحص.",
        "Platform scope": "نطاق المنصة",
        "Operational snapshot": "ملخص تشغيلي",
        "Current records by core product area.": "السجلات الحالية بحسب أقسام المنتج الأساسية.",
        "active universities": "جامعات نشطة",
        "active courses": "مساقات نشطة",
        "Content mix": "توزيع المحتوى",
        "Published resource types": "أنواع المصادر المنشورة",
        "Distribution across the verified knowledge base.": "التوزيع ضمن قاعدة المعرفة الموثقة.",
        "No published content": "لا يوجد محتوى منشور",
        "Resource distribution will appear after the first publication.": "سيظهر توزيع المصادر بعد أول عملية نشر.",
        "Trust signals": "مؤشرات الموثوقية",
        "Resource quality": "جودة المصادر",
        "Verification, file readiness, and community validation.": "التوثيق وجاهزية الملفات وتحقق المجتمع.",
        "verified": "موثّق",
        "Average rating": "متوسط التقييم",
        "File scan readiness": "جاهزية فحص الملفات",
        "of all resources validated": "من جميع المصادر تم التحقق منها",
        "community quality signals": "مؤشرات جودة من المجتمع",
        "Representative items": "المصادر النموذجية",
        "flagged as course exemplars": "محددة كنماذج للمساق",
        "Knowledge activity": "نشاط المحتوى المعرفي",
        "Top courses": "أبرز المساقات",
        "Ranked by published resource volume.": "مرتّبة حسب عدد المصادر المنشورة.",
        "Manage courses": "إدارة المساقات",
        "No active courses": "لا توجد مساقات نشطة",
        "Add a course to begin measuring engagement.": "أضف مساقاً لبدء قياس التفاعل.",
        "No ratings": "لا توجد تقييمات",
        "Community leaders": "روّاد المجتمع",
        "Published work and community usefulness.": "المحتوى المنشور ومدى فائدته للمجتمع.",
        "Manage users": "إدارة المستخدمين",
        "No contributors yet": "لا يوجد مساهمون بعد",
        "Contributor rankings appear after resources are published.": "سيظهر ترتيب المساهمين بعد نشر المصادر.",
        "Recent platform activity": "أحدث نشاطات المنصة",
        "New content, opportunity interest, and audited changes.": "المحتوى الجديد والاهتمام بالفرص والتغييرات المدققة.",
        "Latest resources": "أحدث المصادر",
        "Newest community submissions": "أحدث مشاركات المجتمع",
        "All": "الكل",
        "No resources have been submitted.": "لم تُرفع أي مصادر بعد.",
        "Recent registrations": "أحدث التسجيلات",
        "Latest opportunity interest": "أحدث الاهتمامات بالفرص",
        "No opportunity registrations yet.": "لا توجد تسجيلات في الفرص بعد.",
        "Audit activity": "نشاط التدقيق",
        "Latest administrative events": "أحدث الأحداث الإدارية",
        "No administrative activity recorded.": "لم يُسجّل أي نشاط إداري.",
        "Recommended videos": "فيديوهات موصى بها",
        "External resources": "مصادر خارجية",
        "Rejected": "مرفوض",
        "Archived": "مؤرشف",
        "Draft": "مسودة",
        "Scan ready": "الفحص مكتمل",
        "Scan pending": "الفحص معلّق",
        "Threat detected": "تم اكتشاف تهديد",
        "Scan failed": "فشل الفحص",
        "Waitlisted": "على قائمة الانتظار",
        "Cancelled": "ملغى",
        "Attended": "تم الحضور",
        "Needs attention": "يحتاج إلى مراجعة"
    }));

    const placeholderArabic = new Map(Object.entries({
        "Search resources": "ابحث في المصادر",
        "Search courses": "ابحث في المساقات",
        "Search by name or email": "ابحث بالاسم أو البريد الإلكتروني",
        "Search universities": "ابحث في الجامعات",
        "Search opportunities": "ابحث في الفرص",
        "Ask a question about your active course...": "اسأل سؤالاً عن مساقك الحالي...",
        "Ask about this course...": "اسأل عن هذا المساق...",
        "Ask about this course": "اسأل عن هذا المساق",
        "Code, name, or instructor": "رمز المساق أو اسمه أو المدرّس",
        "Title, course, or uploader": "العنوان أو المساق أو صاحب المصدر",
        "Try MATH 201, algorithms, or an instructor…": "جرّب MATH 201 أو الخوارزميات أو اسم المدرّس…"
    }));

    function translateDynamicText(value) {
        const rules = [
            [/^(\d+) selected$/, "$1 محدد"],
            [/^(\d+) selected in this major$/, "$1 محدد في هذا التخصص"],
            [/^Search across (\d+) selected courses$/, "ابحث في $1 من المساقات المحددة"],
            [/^(\d+) selected · click a course to make it active for the AI tutor$/, "$1 محدد · اختر مساقاً لتفعيله للمدرّس الذكي"],
            [/^(\d+) courses?$/, "$1 مساق"],
            [/^(\d+) course resources$/, "$1 من مصادر المساق"],
            [/^(\d+) trusted (?:materials|resources|sources)$/, "$1 من المصادر الموثوقة"],
            [/^(\d+) sources available$/, "$1 مصدراً متاحاً"],
            [/^(\d+) ratings?$/, "$1 تقييماً"],
            [/^(\d+) helpful$/, "$1 صوت مفيد"],
            [/^(\d+) of (\d+) complete$/, "$1 من $2 مكتمل"],
            [/^Grounded in (.+) only$/, "يستند إلى $1 فقط"],
            [/^Suggested for (.+)$/, "مقترح لمساق $1"],
            [/^Exam (.+)$/, "الامتحان $1"]
        ];
        for (const [pattern, replacement] of rules) {
            if (pattern.test(value)) return value.replace(pattern, replacement);
        }
        return null;
    }

    function rememberAttribute(element, name) {
        let values = originalAttributes.get(element);
        if (!values) {
            values = new Map();
            originalAttributes.set(element, values);
        }
        if (!values.has(name)) values.set(name, element.getAttribute(name) || "");
        return values.get(name);
    }

    function translateTextNode(node, language) {
        if (!originalText.has(node)) originalText.set(node, node.nodeValue || "");
        const source = originalText.get(node);
        if (language === "en") {
            node.nodeValue = source;
            return;
        }

        const trimmed = source.trim();
        const translated = arabicText.get(trimmed) || translateDynamicText(trimmed);
        if (!translated) return;
        const leading = source.match(/^\s*/)?.[0] || "";
        const trailing = source.match(/\s*$/)?.[0] || "";
        node.nodeValue = `${leading}${translated}${trailing}`;
    }

    function applyLanguage(language, persist = true) {
        const next = language === "ar" ? "ar" : "en";
        root.lang = next;
        root.dir = next === "ar" ? "rtl" : "ltr";
        root.dataset.language = next;

        document.querySelectorAll("[data-i18n]").forEach((element) => {
            const key = element.dataset.i18n;
            const value = strings[next]?.[key];
            if (value) element.textContent = value;
        });

        document.querySelectorAll("[data-i18n-placeholder]").forEach((element) => {
            const key = element.dataset.i18nPlaceholder;
            const value = strings[next]?.[key];
            if (value) element.setAttribute("placeholder", value);
        });

        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
            acceptNode(node) {
                const parent = node.parentElement;
                if (!parent || parent.closest("script, style, code, pre, [data-no-translate]") || parent.hasAttribute("data-i18n")) {
                    return NodeFilter.FILTER_REJECT;
                }
                return node.nodeValue?.trim() ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT;
            }
        });
        const nodes = [];
        while (walker.nextNode()) nodes.push(walker.currentNode);
        nodes.forEach((node) => translateTextNode(node, next));

        document.querySelectorAll("input[placeholder], textarea[placeholder]").forEach((element) => {
            if (element.hasAttribute("data-i18n-placeholder")) return;
            const source = rememberAttribute(element, "placeholder");
            element.setAttribute("placeholder", next === "ar" ? placeholderArabic.get(source) || translateDynamicText(source) || source : source);
        });

        document.querySelectorAll("[aria-label], [title]").forEach((element) => {
            ["aria-label", "title"].forEach((name) => {
                if (!element.hasAttribute(name) || element.hasAttribute(`data-i18n-${name}`)) return;
                const source = rememberAttribute(element, name);
                const translated = arabicText.get(source) || translateDynamicText(source);
                element.setAttribute(name, next === "ar" ? translated || source : source);
            });
        });

        document.querySelectorAll("[data-language-toggle]").forEach((button) => {
            const label = button.querySelector(".language-toggle-label");
            if (label) label.textContent = next === "ar" ? "EN" : "ع";
            button.setAttribute("aria-label", next === "ar" ? "Switch to English" : "التبديل إلى العربية");
            button.title = next === "ar" ? "English" : "العربية";
        });

        if (persist) {
            try { localStorage.setItem(storageKey, next); } catch (_) { /* Optional preference only. */ }
        }
        document.dispatchEvent(new CustomEvent("masdar:languageChanged", { detail: { language: next } }));
    }

    function initialize() {
        let saved = root.dataset.language || "en";
        try { saved = localStorage.getItem(storageKey) || saved; } catch (_) { /* Use server/default language. */ }
        applyLanguage(saved, false);
        document.querySelectorAll("[data-language-toggle]").forEach((button) => {
            button.addEventListener("click", () => applyLanguage(root.dataset.language === "ar" ? "en" : "ar"));
        });
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initialize, { once: true });
    else initialize();

    window.MasdarI18n = Object.freeze({ applyLanguage });
})();
