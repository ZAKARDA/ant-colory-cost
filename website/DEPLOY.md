# Ant Colony Colors website (Firebase Hosting)

Pages: `/` home, `/support` contact, `/privacy` privacy policy, `/terms` terms of use, `/app-ads.txt` (AdMob).
Edit text in `build_pages.py`, then run `python3 build_pages.py` to regenerate `public/`.

## First deploy (run on your Mac, in Terminal)
1. Install the CLI once:            npm install -g firebase-tools
2. Log in once:                     firebase login
3. Create the project (or create it in console.firebase.google.com):
                                     firebase projects:create ant-colony-colors --display-name "Ant Colony Colors"
   If the ID is taken, choose another (e.g. ant-colony-colors-app) and put it in `.firebaserc`.
4. Deploy:                          cd "~/Documents/ChatGPT/colony flow/colony-flow-clean/website" && firebase deploy --only hosting

Your site will be at https://ant-colony-colors.web.app

## Links to paste
- App Store Connect > Privacy Policy URL: https://ant-colony-colors.web.app/privacy
- App Store Connect > Support URL:        https://ant-colony-colors.web.app/support
- App Store Connect > Marketing URL:      https://ant-colony-colors.web.app
- App Store Connect > App Information > License Agreement: Apple standard EULA (our /terms adds to it)
- AdMob > Apps > app-ads.txt: add https://ant-colony-colors.web.app as the developer website in App Store Connect,
  then AdMob verifies https://ant-colony-colors.web.app/app-ads.txt
