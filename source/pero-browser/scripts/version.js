const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function getLatestTag() {
  try {
    return execSync('git describe --tags --abbrev=0').toString().trim();
  } catch (error) {
    console.warn('Could not find any tags. Defaulting to v0.');
    return 'v0';
  }
}

function getCommitCountSinceTag(tag) {
  return execSync(`git rev-list ${tag}..HEAD --count`).toString().trim();
}

console.log('🚀 Starting browser extension versioning...');

const latestTag = getLatestTag();
const majorVersion = latestTag.replace('v', '');
const commitCount = getCommitCountSinceTag(latestTag);

const newVersion = `${majorVersion}.${commitCount}`;

console.log(`✅ Latest tag: ${latestTag}`);
console.log(`✅ Commits since tag: ${commitCount}`);
console.log(`📦 New extension version: ${newVersion}`);

try {
  const manifestPath = path.join(__dirname, '..', 'public', 'manifest.json');
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));

  manifest.version = newVersion;

  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
  console.log('🎉 Successfully updated manifest.json!');
} catch (error) {
  console.error('❌ Error updating manifest.json:', error);
  process.exit(1);
}