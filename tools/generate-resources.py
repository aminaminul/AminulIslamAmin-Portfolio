#!/usr/bin/env python3
"""Generates Resources/SharedResource.resx and SharedResource.bn.resx.

The neutral file maps each UI key to itself (English), and the Bangla file supplies
translations. Missing translations fall back to the English key at runtime.
"""
from pathlib import Path
from xml.sax.saxutils import escape
import subprocess

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src" / "Portfolio.Web"
OUT = WEB / "Resources"

keys = set()
for folder in ("Views", "Areas/Admin/Views"):
    for path in (WEB / folder).rglob("*.cshtml"):
        text = path.read_text(encoding="utf-8")
        for line in text.splitlines():
            idx = 0
            while True:
                start = line.find('L["', idx)
                if start == -1:
                    break
                end = line.find('"]', start + 3)
                if end == -1:
                    break
                keys.add(line[start + 3:end])
                idx = end + 2

data_annotations = {
    "The {0} field is required.": "{0} ফিল্ডটি আবশ্যক।",
    "The {0} field is not a valid e-mail address.": "{0} ফিল্ডে একটি বৈধ ইমেইল ঠিকানা দিন।",
    "The field {0} must be a string with a maximum length of {1}.": "{0} ফিল্ডে সর্বোচ্চ {1}টি অক্ষর থাকতে পারে।",
    "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.": "{0} ফিল্ডে সর্বনিম্ন {2} এবং সর্বোচ্চ {1}টি অক্ষর থাকতে হবে।",
    "The field {0} must be a string or array type with a minimum length of '{1}'.": "{0} ফিল্ডে সর্বনিম্ন {1}টি অক্ষর থাকতে হবে।",
    "The field {0} must be between {1} and {2}.": "{0} ফিল্ড {1} থেকে {2} এর মধ্যে হতে হবে।",
    "The field {0} must be a number.": "{0} ফিল্ডে একটি সংখ্যা দিন।",
    "The value '{0}' is not valid for {1}.": "'{0}' মানটি {1} এর জন্য বৈধ নয়।",
    "'{0}' and '{1}' do not match.": "'{0}' এবং '{1}' মিলছে না।",
}

bn = {
    "A complete overview of my experience": "আমার অভিজ্ঞতার পূর্ণ বিবরণ",
    "A selection of work, side projects and experiments. Only published projects are listed.": "কাজ, সাইড প্রজেক্ট ও পরীক্ষা-নিরীক্ষার নির্বাচিত সংগ্রহ। শুধু প্রকাশিত প্রকল্প এখানে দেখা যাবে।",
    "About": "পরিচিতি",
    "Abstract": "সারসংক্ষেপ",
    "Academic background": "শিক্ষাগত পটভূমি",
    "Achievements": "অর্জন",
    "Admin": "অ্যাডমিন",
    "Admin sign in": "অ্যাডমিন সাইন ইন",
    "All": "সব",
    "All categories": "সব বিভাগ",
    "All projects": "সব প্রকল্প",
    "All research": "সব গবেষণা",
    "All rights reserved.": "সর্বস্বত্ব সংরক্ষিত।",
    "All services": "সব সেবা",
    "All years": "সব বছর",
    "Apply filters": "ফিল্টার প্রয়োগ করুন",
    "Archived": "আর্কাইভ করা",
    "Authors": "লেখক",
    "Back to home": "হোমে ফিরুন",
    "Biography coming soon": "পরিচিতি শীঘ্রই আসছে",
    "Breadcrumb": "পথনির্দেশ",
    "Built with ASP.NET Core": "ASP.NET Core দিয়ে তৈরি",
    "Category": "বিভাগ",
    "Certifications": "সার্টিফিকেশন",
    "Certifications & credentials": "সার্টিফিকেশন ও সনদ",
    "Clear filters": "ফিল্টার মুছুন",
    "Completed": "সম্পন্ন",
    "Conclusion": "উপসংহার",
    "Contact": "যোগাযোগ",
    "Contact details": "যোগাযোগের তথ্য",
    "Contact page": "যোগাযোগের পাতা",
    "Datasets": "ডেটাসেট",
    "Discussion": "আলোচনা",
    "Download PDF": "PDF ডাউনলোড",
    "Education": "শিক্ষা",
    "Email": "ইমেইল",
    "Email address": "ইমেইল ঠিকানা",
    "Email delivery is not configured, so your message is saved to the site inbox instead.": "ইমেইল ডেলিভারি কনফিগার করা নেই, তাই আপনার বার্তা সাইটের ইনবক্সে সংরক্ষণ করা হবে।",
    "Experience": "অভিজ্ঞতা",
    "Experiments": "পরীক্ষা-নিরীক্ষা",
    "Expires": "মেয়াদ শেষ",
    "Explore": "দেখুন",
    "Explore all skills": "সব দক্ষতা দেখুন",
    "Filter by category": "বিভাগ অনুযায়ী ফিল্টার",
    "Filtered by tag": "ট্যাগ অনুযায়ী ফিল্টার",
    "Findings": "ফলাফল",
    "Footer navigation": "ফুটার নেভিগেশন",
    "From": "শুরু",
    "Full experience": "সম্পূর্ণ অভিজ্ঞতা",
    "Future work": "ভবিষ্যৎ কাজ",
    "Gallery": "গ্যালারি",
    "Get in touch": "যোগাযোগ করুন",
    "Have a project or a role in mind? Send a message and I will get back to you.": "কোনো প্রকল্প বা পদ মাথায় আছে? বার্তা পাঠান, আমি উত্তর দেব।",
    "Have a project that needs these skills? Let's talk about it.": "এই দক্ষতাগুলো প্রয়োজন এমন প্রকল্প আছে? কথা বলি।",
    "Have a question or a project in mind? Send a message to {0}.": "প্রশ্ন বা প্রকল্পের পরিকল্পনা আছে? {0}-কে বার্তা পাঠান।",
    "Have a similar project?": "অনুরূপ প্রকল্প আছে?",
    "Home": "হোম",
    "How I can help": "আমি যেভাবে সহায়তা করতে পারি",
    "I agree to be contacted about this message.": "এই বার্তার বিষয়ে যোগাযোগে সম্মত আছি।",
    "I'm happy to discuss requirements and how I can help.": "প্রয়োজনীয়তা এবং কীভাবে সহায়তা করতে পারি, তা নিয়ে আলোচনা করতে প্রস্তুত।",
    "ID": "আইডি",
    "In progress": "চলমান",
    "Introduction": "ভূমিকা",
    "Issued": "ইস্যু করা হয়েছে",
    "Journal": "জার্নাল",
    "Key features": "প্রধান বৈশিষ্ট্য",
    "Keywords": "কীওয়ার্ড",
    "Language": "ভাষা",
    "Let's build something together": "চলুন একসাথে কিছু তৈরি করি",
    "Limitations": "সীমাবদ্ধতা",
    "Live demo": "লাইভ ডেমো",
    "Location": "অবস্থান",
    "Main navigation": "প্রধান নেভিগেশন",
    "Maintained": "রক্ষণাবেক্ষণাধীন",
    "Menu": "মেনু",
    "Message": "বার্তা",
    "Methodology": "পদ্ধতি",
    "Mobile navigation": "মোবাইল নেভিগেশন",
    "More about me": "আমার সম্পর্কে আরও",
    "More details": "আরও বিস্তারিত",
    "Next": "পরবর্তী",
    "No matching projects": "কোনো মিলে যাওয়া প্রকল্প নেই",
    "No research found": "কোনো গবেষণা পাওয়া যায়নি",
    "No services listed yet": "এখনো কোনো সেবা যোগ করা হয়নি",
    "No skills listed yet": "এখনো কোনো দক্ষতা যোগ করা হয়নি",
    "Only signed-in administrators can see this page.": "শুধু সাইন-ইন করা অ্যাডমিন এই পৃষ্ঠাটি দেখতে পারেন।",
    "Overview": "সারসংক্ষেপ",
    "Page": "পৃষ্ঠা",
    "Pagination": "পৃষ্ঠা বিভাজন",
    "Phone": "ফোন",
    "Planned": "পরিকল্পিত",
    "Portfolio": "পোর্টফোলিও",
    "Present": "বর্তমান",
    "Preview": "প্রিভিউ",
    "Previous": "পূর্ববর্তী",
    "Professional experience": "পেশাগত অভিজ্ঞতা",
    "Profiles": "প্রোফাইল",
    "Project details": "প্রকল্পের বিবরণ",
    "Projects": "প্রকল্প",
    "Projects are coming soon": "প্রকল্প শীঘ্রই আসছে",
    "Publication details": "প্রকাশনার বিবরণ",
    "Published": "প্রকাশিত",
    "Published projects will appear here as soon as they are ready.": "প্রকাশিত প্রকল্প প্রস্তুত হলে এখানে দেখা যাবে।",
    "Published projects will be listed here.": "প্রকাশিত প্রকল্প এখানে তালিকাভুক্ত হবে।",
    "Published research matching these filters will appear here.": "এই ফিল্টারের সাথে মিলে যাওয়া প্রকাশিত গবেষণা এখানে দেখা যাবে।",
    "Published research, methods and findings. Drafts and archived work are not listed.": "প্রকাশিত গবেষণা, পদ্ধতি ও ফলাফল। ড্রাফট ও আর্কাইভ করা কাজ এখানে নেই।",
    "Read research": "গবেষণা পড়ুন",
    "References": "তথ্যসূত্র",
    "Related projects": "সম্পর্কিত প্রকল্প",
    "Related research": "সম্পর্কিত গবেষণা",
    "Remove": "সরান",
    "Repository": "রিপোজিটরি",
    "Research": "গবেষণা",
    "Research & publications": "গবেষণা ও প্রকাশনা",
    "Resume": "সিভি",
    "Search": "খুঁজুন",
    "Search projects": "প্রকল্প খুঁজুন",
    "Search projects or technologies": "প্রকল্প বা প্রযুক্তি খুঁজুন",
    "Search research": "গবেষণা খুঁজুন",
    "Search titles, abstracts and keywords": "শিরোনাম, সারসংক্ষেপ ও কীওয়ার্ড খুঁজুন",
    "Selected achievements": "নির্বাচিত অর্জন",
    "Selected projects": "নির্বাচিত প্রকল্প",
    "Send message": "বার্তা পাঠান",
    "Services": "সেবা",
    "Services added in the admin panel will be listed here.": "অ্যাডমিন প্যানেল থেকে যোগ করা সেবা এখানে দেখা যাবে।",
    "Services offered by": "সেবা প্রদান করছেন",
    "Share your goals and requirements, and I will reply with the next steps.": "আপনার লক্ষ্য ও প্রয়োজন জানান, আমি পরবর্তী পদক্ষেপ জানাব।",
    "Skill categories and technologies added in the admin panel will appear here.": "অ্যাডমিন প্যানেল থেকে যোগ করা দক্ষতা ও প্রযুক্তি এখানে দেখা যাবে।",
    "Skills": "দক্ষতা",
    "Skills & technologies": "দক্ষতা ও প্রযুক্তি",
    "Skills added in the admin panel appear here, grouped by category.": "অ্যাডমিন প্যানেলে যোগ করা দক্ষতা বিভাগ অনুযায়ী এখানে দেখা যায়।",
    "Skip to content": "মূল কনটেন্টে যান",
    "Social profiles": "সোশ্যাল প্রোফাইল",
    "Stack": "টেক স্ট্যাক",
    "Started": "শুরু",
    "Status": "অবস্থা",
    "Subject": "বিষয়",
    "Tags": "ট্যাগ",
    "Technologies": "প্রযুক্তি",
    "Tell me about your project": "আপনার প্রকল্প সম্পর্কে বলুন",
    "Tell me about your project, role or question.": "আপনার প্রকল্প, পদ বা প্রশ্ন সম্পর্কে লিখুন।",
    "Testimonials": "প্রশংসাপত্র",
    "The contact form is currently closed": "যোগাযোগ ফর্ম এখন বন্ধ",
    "The owner has not published an about section yet.": "মালিক এখনো পরিচিতি অংশ প্রকাশ করেননি।",
    "The problem": "সমস্যা",
    "The solution": "সমাধান",
    "The tools and technologies {0} works with.": "{0} যেসব টুল ও প্রযুক্তি ব্যবহার করেন।",
    "Timeline": "সময়কাল",
    "Toggle theme": "থিম পরিবর্তন",
    "Tools": "টুলস",
    "Try a different search term or clear the filters.": "অন্য শব্দে খুঁজুন অথবা ফিল্টার মুছুন।",
    "Venue": "স্থান",
    "Verify credential": "সনদ যাচাই করুন",
    "View projects": "প্রকল্প দেখুন",
    "View publication": "প্রকাশনা দেখুন",
    "What collaborators say": "সহকর্মীরা যা বলেন",
    "Where I have worked": "যেখানে কাজ করেছি",
    "Year": "বছর",
    "You can still reach out by email.": "ইমেইলে যোগাযোগ করতে পারেন।",
    "Your details are only used to reply to your message.": "আপনার তথ্য শুধু উত্তর দেওয়ার জন্য ব্যবহৃত হয়।",
    "Your name": "আপনার নাম",
    "of": "এর",
    "project": "প্রকল্প",
    "projectsPlural": "প্রকল্প",
    "publication": "প্রকাশনা",
    "publications": "প্রকাশনা",
}

bn.update({k: v for k, v in data_annotations.items()})
all_keys = sorted(keys | set(bn.keys()))

HEADER = """<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence><xsd:element name="value" type="xsd:string" minOccurs="0" /></xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType><xsd:attribute name="alias" type="xsd:string" /><xsd:attribute name="name" type="xsd:string" /></xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence><xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" /></xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
"""


def write(path: Path, translations: dict[str, str]) -> None:
    parts = [HEADER]
    for key in all_keys:
        value = translations.get(key, key)
        parts.append(
            "  <data name=\"{0}\" xml:space=\"preserve\">\n    <value>{1}</value>\n  </data>\n".format(
                escape(key, {'"': "&quot;"}), escape(value)
            )
        )
    parts.append("</root>\n")
    path.write_text("".join(parts), encoding="utf-8")


OUT.mkdir(exist_ok=True)
write(OUT / "SharedResource.resx", {})
write(OUT / "SharedResource.bn.resx", bn)
missing = sorted(keys - set(bn.keys()))
print(f"{len(all_keys)} keys written; {len(missing)} untranslated")
for key in missing:
    print("  missing:", key)
