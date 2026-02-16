#!/bin/bash
cd /Users/manasnayak/Projects/GitHub/twf-pulse

# More icons batch 2
cp docs/svg/emoji_u26a0.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/warning.svg
cp docs/svg/emoji_u2139.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/info.svg
cp docs/svg/emoji_u2753.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/question.svg
cp docs/svg/emoji_u2757.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/exclamation.svg
cp docs/svg/emoji_u2b50.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/star.svg

# Objects
cp docs/svg/emoji_u1f4bb.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/laptop.svg
cp docs/svg/emoji_u1f4f1.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/mobile.svg
cp docs/svg/emoji_u1f4e7.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/email.svg
cp docs/svg/emoji_u1f4c1.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/folder.svg
cp docs/svg/emoji_u1f4c2.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/folder-open.svg
cp docs/svg/emoji_u1f4be.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/save.svg
cp docs/svg/emoji_u1f4c4.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/document.svg
cp docs/svg/emoji_u1f5d1.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/trash.svg
cp docs/svg/emoji_u270f.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/pencil.svg
cp docs/svg/emoji_u1f58a.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/pen.svg
cp docs/svg/emoji_u1f527.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/wrench.svg
cp docs/svg/emoji_u2699.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/settings.svg
cp docs/svg/emoji_u1f512.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/locked.svg
cp docs/svg/emoji_u1f513.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/unlocked.svg
cp docs/svg/emoji_u1f511.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/key.svg

# Time
cp docs/svg/emoji_u23f0.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/alarm.svg
cp docs/svg/emoji_u23f1.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/stopwatch.svg
cp docs/svg/emoji_u231b.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/hourglass.svg

# Locations
cp docs/svg/emoji_u1f4cd.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/location.svg
cp docs/svg/emoji_u1f30d.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/globe.svg
cp docs/svg/emoji_u1f3e0.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/house.svg
cp docs/svg/emoji_u1f3e2.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/office.svg
cp docs/svg/emoji_u1f3eb.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/school.svg

# Emotions
cp docs/svg/emoji_u1f600.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/grinning.svg
cp docs/svg/emoji_u1f603.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/grinning-big.svg
cp docs/svg/emoji_u1f604.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/smile.svg
cp docs/svg/emoji_u1f60a.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/smiling.svg
cp docs/svg/emoji_u1f914.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/thinking.svg
cp docs/svg/emoji_u1f622.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/sad.svg
cp docs/svg/emoji_u1f615.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/confused.svg
cp docs/svg/emoji_u1f44c.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/ok-hand.svg
cp docs/svg/emoji_u2764.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/heart.svg
cp docs/svg/emoji_u1f4af.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/hundred.svg

# Celebration
cp docs/svg/emoji_u1f389.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/party.svg
cp docs/svg/emoji_u1f38a.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/confetti.svg
cp docs/svg/emoji_u1f3c6.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/trophy.svg
cp docs/svg/emoji_u1f396.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/medal-sports.svg
cp docs/svg/emoji_u26a1.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/bolt.svg
cp docs/svg/emoji_u1f4ac.svg src/TechWayFit.Pulse.Web/wwwroot/images/icons/chat.svg

echo "Copied additional icons"
ls -1 src/TechWayFit.Pulse.Web/wwwroot/images/icons/ | wc -l | xargs echo "Total icons:"
