#!/bin/bash
# CLI-only deploy: adds the "Ant Colony Colors" site to an existing Firebase project
# (no new project needed). Everything is logged to deploy-log.txt.
cd "$(dirname "$0")" || exit 1
export PATH="/usr/local/bin:/opt/homebrew/bin:$HOME/.local/bin:$PATH"
exec > >(tee deploy-log.txt) 2>&1
ACCOUNT="elqznysf@gmail.com"        # account that already has Firebase projects
HOST_PROJECT="admob-51589"          # existing project ("ADMOB") that will hold the site
echo "=== Ant Colony Colors website deploy $(date) ==="
firebase --version || { echo "FAILED: firebase CLI missing"; read -r; exit 1; }

if ! firebase login:list 2>/dev/null | grep -q "$ACCOUNT"; then
  firebase logout >/dev/null 2>&1
  echo
  echo ">>> Chrome will open. Choose $ACCOUNT and click Allow. <<<"
  echo
  firebase login --reauth
fi
firebase login:list
if ! firebase login:list 2>/dev/null | grep -q "$ACCOUNT"; then
  echo "FAILED: please sign in with $ACCOUNT (you chose another account)."; read -r; exit 1
fi

SITE=""
for id in ant-colony-colors ant-colony-colors-app ant-colony-colors-game antcolonycolors; do
  if firebase hosting:sites:get "$id" --project "$HOST_PROJECT" >/dev/null 2>&1; then SITE="$id"; echo "Using existing site $id"; break; fi
  echo "Creating site $id ..."
  if firebase hosting:sites:create "$id" --project "$HOST_PROJECT"; then SITE="$id"; break; fi
done
[ -z "$SITE" ] && { echo "FAILED: could not create a hosting site"; read -r; exit 1; }

printf '{\n  "projects": {\n    "default": "%s"\n  }\n}\n' "$HOST_PROJECT" > .firebaserc
/usr/bin/sed -i '' "s/\"site\": \"[^\"]*\", //; s/\"hosting\": {/\"hosting\": { \"site\": \"$SITE\", /" firebase.json
cat firebase.json | head -3

if firebase deploy --only hosting --project "$HOST_PROJECT"; then
  echo "RESULT: SUCCESS https://$SITE.web.app"
else
  echo "RESULT: FAILED"
fi
echo
echo "Done. Tell Claude \"sala\". You can close this window."
read -r
