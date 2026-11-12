# Generates the static pages in public/. Edit the content below, then run: python3 build_pages.py
APP = "Ant Colony Colors"
DEV = "Ant Colony Colors"
EMAIL = "zaykarda@yahoo.com"
UPDATED = "November 9, 2026"
NAV = [("index", "Home"), ("support", "Contact"), ("privacy", "Privacy"), ("terms", "Terms")]

def page(slug, title, body, desc):
    links = "".join(f'<a href="/{"" if s == "index" else s}"{" class=on" if s == slug else ""}>{n}</a>' for s, n in NAV)
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title}</title>
<meta name="description" content="{desc}">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700;800&display=swap" rel="stylesheet">
<link rel="stylesheet" href="/styles.css">
</head>
<body>
<header><div class="wrap nav"><a class="brand" href="/"><img src="/favicon.svg" alt="">{APP}</a><nav class="links">{links}</nav></div></header>
<main class="wrap">
{body}
</main>
<footer><div class="wrap"><div>&copy; 2026 {DEV}. All rights reserved.</div><div><a href="/support">Contact</a><a href="/privacy">Privacy Policy</a><a href="/terms">Terms of Use</a></div></div></footer>
</body>
</html>
"""

home = f"""<section class="hero">
<span class="spark"></span><span class="spark"></span><span class="spark"></span><span class="spark"></span><span class="spark"></span>
<div class="sunburst"></div><div class="glow"></div>
<img class="mascot" src="/favicon.svg" alt="{APP} icon">
<div class="eyebrow">A relaxing color puzzle for iPhone</div>
<h1>Ant Colony <span class="gold">Colors</span></h1>
<p class="lead">Guide colonies of tiny ants to paint beautiful pixel pictures, one color at a time. Calm, clever and made to relax.</p>
<a class="btn" href="/support">Get help</a><a class="btn ghost" href="/privacy">Privacy Policy</a>
<div class="pixels" aria-hidden="true"><i style="--c:#fbefd2;--n:0"></i><i style="--c:#fbefd2;--n:1"></i><i style="--c:#fbefd2;--n:2"></i><i style="--c:#fbefd2;--n:3"></i><i style="--c:#fbefd2;--n:4"></i><i style="--c:#fbefd2;--n:5"></i><i style="--c:#fbefd2;--n:6"></i><i style="--c:#fbefd2;--n:7"></i><i style="--c:#fbefd2;--n:8"></i><i style="--c:#fbefd2;--n:9"></i><i style="--c:#fbefd2;--n:10"></i><i style="--c:#fbefd2;--n:11"></i><i style="--c:#fbefd2;--n:12"></i><i style="--c:#fbefd2;--n:13"></i><i style="--c:#d4584a;--n:14"></i><i style="--c:#d4584a;--n:15"></i><i style="--c:#fbefd2;--n:16"></i><i style="--c:#fbefd2;--n:17"></i><i style="--c:#fbefd2;--n:18"></i><i style="--c:#fbefd2;--n:19"></i><i style="--c:#d4584a;--n:20"></i><i style="--c:#d4584a;--n:21"></i><i style="--c:#fbefd2;--n:22"></i><i style="--c:#fbefd2;--n:23"></i><i style="--c:#fbefd2;--n:24"></i><i style="--c:#d4584a;--n:25"></i><i style="--c:#f08a7a;--n:26"></i><i style="--c:#f08a7a;--n:27"></i><i style="--c:#d4584a;--n:28"></i><i style="--c:#fbefd2;--n:29"></i><i style="--c:#fbefd2;--n:30"></i><i style="--c:#d4584a;--n:31"></i><i style="--c:#f08a7a;--n:32"></i><i style="--c:#f08a7a;--n:33"></i><i style="--c:#d4584a;--n:34"></i><i style="--c:#fbefd2;--n:35"></i><i style="--c:#fbefd2;--n:36"></i><i style="--c:#d4584a;--n:37"></i><i style="--c:#f08a7a;--n:38"></i><i style="--c:#f08a7a;--n:39"></i><i style="--c:#f08a7a;--n:40"></i><i style="--c:#d4584a;--n:41"></i><i style="--c:#d4584a;--n:42"></i><i style="--c:#f08a7a;--n:43"></i><i style="--c:#f08a7a;--n:44"></i><i style="--c:#f08a7a;--n:45"></i><i style="--c:#d4584a;--n:46"></i><i style="--c:#fbefd2;--n:47"></i><i style="--c:#fbefd2;--n:48"></i><i style="--c:#fbefd2;--n:49"></i><i style="--c:#d4584a;--n:50"></i><i style="--c:#f08a7a;--n:51"></i><i style="--c:#f08a7a;--n:52"></i><i style="--c:#f08a7a;--n:53"></i><i style="--c:#f08a7a;--n:54"></i><i style="--c:#f08a7a;--n:55"></i><i style="--c:#f08a7a;--n:56"></i><i style="--c:#d4584a;--n:57"></i><i style="--c:#fbefd2;--n:58"></i><i style="--c:#fbefd2;--n:59"></i><i style="--c:#fbefd2;--n:60"></i><i style="--c:#fbefd2;--n:61"></i><i style="--c:#fbefd2;--n:62"></i><i style="--c:#d4584a;--n:63"></i><i style="--c:#f08a7a;--n:64"></i><i style="--c:#f08a7a;--n:65"></i><i style="--c:#f08a7a;--n:66"></i><i style="--c:#f08a7a;--n:67"></i><i style="--c:#d4584a;--n:68"></i><i style="--c:#fbefd2;--n:69"></i><i style="--c:#fbefd2;--n:70"></i><i style="--c:#fbefd2;--n:71"></i><i style="--c:#fbefd2;--n:72"></i><i style="--c:#fbefd2;--n:73"></i><i style="--c:#fbefd2;--n:74"></i><i style="--c:#fbefd2;--n:75"></i><i style="--c:#d4584a;--n:76"></i><i style="--c:#f08a7a;--n:77"></i><i style="--c:#f08a7a;--n:78"></i><i style="--c:#d4584a;--n:79"></i><i style="--c:#fbefd2;--n:80"></i><i style="--c:#fbefd2;--n:81"></i><i style="--c:#fbefd2;--n:82"></i><i style="--c:#fbefd2;--n:83"></i></div>
</section>
<section class="grid">
<div class="card"><div class="icon">🐜</div><h3>Tap a colony</h3><p>Each colony walks to a free nest and its ants eat the matching tiles on the edge of the picture.</p></div>
<div class="card"><div class="icon">🎨</div><h3>Reveal the picture</h3><p>Outer layers hide the colors underneath. Plan ahead, because your nests are limited.</p></div>
<div class="card"><div class="icon">⭐</div><h3>Daily puzzle</h3><p>A new daily puzzle with streak rewards, combo bonuses and a growing picture collection.</p></div>
</section>"""

support = f"""<article class="doc">
<h1>Contact &amp; Support</h1>
<p class="updated">We usually reply within 2 business days.</p>
<p>Questions, feedback, a bug to report, or a picture idea? We would love to hear from you.</p>
<div class="contact-box"><span class="icon">✉️</span><div><div>Email us at</div><strong><a href="mailto:{EMAIL}?subject={APP}%20support">{EMAIL}</a></strong></div></div>
<p>To help us solve a problem quickly, please include your device model, iOS version, the app version (shown at the bottom of the Settings screen) and the level number if it is about a specific picture.</p>
<h2>Frequently asked questions</h2>
<p><strong>How do I play?</strong> Tap the first ant in a lane. It moves to a free nest and its ants eat the matching tiles on the outside of the picture. Clear every tile to win.</p>
<p><strong>I am stuck. What can I do?</strong> Use Shuffle to reorder the waiting colonies, add an extra nest, or restart the level.</p>
<p><strong>How do I unlock a picture early?</strong> In the Picture Collection, tap a locked picture and watch a short video to unlock it.</p>
<p><strong>Will I lose my progress if I delete the app?</strong> Progress is stored only on your device, so deleting the app also deletes your progress.</p>
<p><strong>How do I change ad privacy choices?</strong> Open Settings in the app and choose Privacy Options (shown where required). You can also manage tracking in iOS Settings &gt; Privacy &amp; Security &gt; Tracking.</p>
<p><strong>How do I delete my data?</strong> We do not keep an account or personal data on our servers. Deleting the app removes all game data from your device. For data held by our advertising partner, see our <a href="/privacy">Privacy Policy</a>.</p>
</article>"""

privacy = f"""<article class="doc">
<h1>Privacy Policy</h1>
<p class="updated">Last updated: {UPDATED}</p>
<p>This Privacy Policy explains how {DEV} ("we", "us") handles information when you use the {APP} mobile game (the "App"). We designed the App to collect as little information as possible.</p>
<h2>1. Information we collect</h2>
<p>We do not ask you to create an account, and we do not collect your name, email address, phone number, contacts, photos or location. Your game progress, coins and settings are stored only on your device.</p>
<h2>2. Advertising (Google AdMob)</h2>
<p>The App is free and shows ads provided by Google AdMob, including app-open, interstitial and optional rewarded video ads. To show and measure ads, Google may collect and process information such as your device's advertising identifier (IDFA, only if you allow tracking), IP address, device type, operating system, app interactions with ads, and approximate location derived from your IP address.</p>
<p>On iOS, the App asks for your permission through Apple's App Tracking Transparency prompt before your advertising identifier can be used for tracking. If you decline, you will still see ads, but they will be less personalized. Where required by law (for example in the EEA, the UK and Switzerland), the App shows a consent message from Google's User Messaging Platform, and you can change your choice at any time from Privacy Options in the App's Settings.</p>
<p>Learn more about how Google uses information: <a href="https://policies.google.com/technologies/partner-sites" rel="noopener">How Google uses information from sites or apps that use its services</a> and <a href="https://policies.google.com/privacy" rel="noopener">Google Privacy Policy</a>.</p>
<h2>3. How information is used</h2>
<ul><li>To show, limit the frequency of, and measure ads.</li><li>To detect and prevent fraud and invalid ad traffic.</li><li>To keep the App working and improve it (for example, crash and performance information collected by the advertising SDK).</li></ul>
<p>We do not sell your personal information and we do not use it for any purpose other than those listed here.</p>
<h2>4. Sharing</h2>
<p>Information described in section 2 is shared with Google as our advertising provider and with the ad networks it works with. We do not share information with anyone else, except if required by law.</p>
<h2>5. Data retention and deletion</h2>
<p>Game data stays on your device until you delete the App. We do not keep a copy. Google retains advertising data according to its own policies. You can reset your advertising identifier or limit tracking in iOS Settings &gt; Privacy &amp; Security &gt; Tracking.</p>
<h2>6. Children</h2>
<p>The App is not directed to children under the age of 13, and we do not knowingly collect personal information from children. If you believe a child has provided us personal information, contact us and we will help remove it.</p>
<h2>7. Your rights</h2>
<p>Depending on where you live (for example under the GDPR or the CCPA/CPRA), you may have the right to access, correct, delete or object to the processing of your personal information and to withdraw consent. Because we do not hold personal data on our own servers, most requests are handled through the App's Privacy Options, your device settings, or directly with Google. You can always contact us for help.</p>
<h2>8. Security</h2>
<p>We rely on the security of iOS and of our advertising provider. No method of transmission or storage is completely secure, but we take reasonable steps to protect information.</p>
<h2>9. Changes to this policy</h2>
<p>We may update this Privacy Policy from time to time. We will post the new version on this page and update the date above.</p>
<h2>10. Contact</h2>
<p>Questions about this policy: <a href="mailto:{EMAIL}">{EMAIL}</a></p>
</article>"""

terms = f"""<article class="doc">
<h1>Terms of Use</h1>
<p class="updated">Last updated: {UPDATED}</p>
<p>These Terms of Use ("Terms") apply to the {APP} mobile game (the "App") provided by {DEV}. By downloading or using the App you agree to these Terms. If you do not agree, please do not use the App.</p>
<h2>1. License</h2>
<p>We grant you a personal, non-exclusive, non-transferable, revocable license to use the App on Apple devices that you own or control, for your own non-commercial entertainment, as permitted by the Usage Rules in the Apple Media Services Terms and Conditions. Apple's <a href="https://www.apple.com/legal/internet-services/itunes/dev/stdeula/" rel="noopener">Licensed Application End User License Agreement</a> also applies.</p>
<h2>2. Virtual items</h2>
<p>Coins, unlocked pictures, streaks and other in-game items have no real-world monetary value, cannot be exchanged for money and cannot be transferred. We may change or rebalance in-game items at any time.</p>
<h2>3. Advertising</h2>
<p>The App is free and supported by ads. Rewarded videos are optional. Ads are provided by third parties, and we are not responsible for the content of third-party ads or the websites they link to.</p>
<h2>4. Acceptable use</h2>
<p>You agree not to copy, modify, reverse engineer, decompile, or create derivative works from the App, not to interfere with its ads or security, and not to use cheats, automation or exploits.</p>
<h2>5. Intellectual property</h2>
<p>The App, including its code, artwork, puzzles, music, sounds and name, is owned by {DEV} or its licensors and is protected by intellectual property laws. Poppins font is used under the SIL Open Font License 1.1.</p>
<h2>6. Updates and availability</h2>
<p>We may update, change or discontinue the App or any feature at any time without notice. Some updates may be required to keep using the App.</p>
<h2>7. Disclaimer</h2>
<p>The App is provided "as is" and "as available", without warranties of any kind, to the fullest extent permitted by law. We do not guarantee that the App will be uninterrupted or error-free.</p>
<h2>8. Limitation of liability</h2>
<p>To the fullest extent permitted by law, {DEV} will not be liable for any indirect, incidental, special or consequential damages, or for any loss of data or progress, arising from your use of the App. Nothing in these Terms limits rights you have under consumer protection laws that cannot be excluded.</p>
<h2>9. Apple</h2>
<p>These Terms are between you and {DEV}, not Apple. Apple is not responsible for the App or its content, has no obligation to provide maintenance or support, and is a third-party beneficiary of these Terms with the right to enforce them against you.</p>
<h2>10. Changes</h2>
<p>We may update these Terms from time to time. Continuing to use the App after changes means you accept the updated Terms.</p>
<h2>11. Contact</h2>
<p>Questions about these Terms: <a href="mailto:{EMAIL}">{EMAIL}</a></p>
</article>"""

notfound = """<section class="hero"><div class="eyebrow">404</div><h1>This page wandered <span class="gold">off</span></h1><p class="lead">Our ants could not find that page.</p><a class="btn" href="/">Back home</a></section>"""

pages = {
    "index": (f"{APP} — Relaxing Ant Color Puzzle", home, "Ant Colony Colors is a relaxing color puzzle game for iPhone."),
    "support": (f"Contact & Support — {APP}", support, "Get help with Ant Colony Colors."),
    "privacy": (f"Privacy Policy — {APP}", privacy, "Privacy Policy for the Ant Colony Colors app."),
    "terms": (f"Terms of Use — {APP}", terms, "Terms of Use for the Ant Colony Colors app."),
    "404": (f"Page not found — {APP}", notfound, "Page not found."),
}
for slug, (title, body, desc) in pages.items():
    with open(f"public/{slug}.html", "w", encoding="utf-8") as f:
        f.write(page(slug, title, body, desc))
print("pages:", ", ".join(pages))
