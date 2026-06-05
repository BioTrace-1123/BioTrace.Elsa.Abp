import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { cpSync, mkdirSync, rmSync } from 'fs';

const hostDir = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const libsDir = path.join(hostDir, 'wwwroot', 'libs');

function parseMapping(filePath) {
  const raw = fs.readFileSync(filePath, 'utf8')
    .replace('module.exports', '')
    .replace('=', '')
    .trim()
    .replace(/;$/, '');
  return Function(`"use strict"; return (${raw})`)();
}

function normalizePattern(pattern) {
  return pattern.replace('@node_modules/', 'node_modules/');
}

function walkFiles(baseDir, relativePattern) {
  const results = [];

  if (!relativePattern.includes('*')) {
    const fullPath = path.join(baseDir, relativePattern);
    if (fs.existsSync(fullPath)) {
      results.push(fullPath);
    }
    return results;
  }

  const parts = relativePattern.split('/');
  const fileNamePattern = parts.pop();
  const dirPath = path.join(baseDir, ...parts);

  if (!fs.existsSync(dirPath)) {
    return results;
  }

  const regex = new RegExp(
    '^' + fileNamePattern.replace(/\./g, '\\.').replace(/\*/g, '.*') + '$'
  );

  for (const entry of fs.readdirSync(dirPath, { withFileTypes: true })) {
    if (entry.isFile() && regex.test(entry.name)) {
      results.push(path.join(dirPath, entry.name));
    }
  }

  return results;
}

function copyFile(sourceFile, destDir) {
  mkdirSync(destDir, { recursive: true });
  cpSync(sourceFile, path.join(destDir, path.basename(sourceFile)));
}

rmSync(libsDir, { recursive: true, force: true });
mkdirSync(libsDir, { recursive: true });

const abpDir = path.join(hostDir, 'node_modules', '@abp');
if (!fs.existsSync(abpDir)) {
  console.error('node_modules/@abp not found. Run npm install first.');
  process.exit(1);
}

for (const pkg of fs.readdirSync(abpDir)) {
  const mappingFile = path.join(abpDir, pkg, 'abp.resourcemapping.js');
  if (!fs.existsSync(mappingFile)) {
    continue;
  }

  const config = parseMapping(mappingFile);
  if (!config?.mappings) {
    continue;
  }

  for (const [srcPattern, destPattern] of Object.entries(config.mappings)) {
    const normalizedSrc = normalizePattern(srcPattern);
    const destDir = path.join(hostDir, destPattern.replace('@libs/', 'wwwroot/libs/'));

    if (normalizedSrc.endsWith('/*')) {
      const sourceDir = path.join(hostDir, normalizedSrc.slice(0, -2));
      if (fs.existsSync(sourceDir)) {
        mkdirSync(destDir, { recursive: true });
        cpSync(sourceDir, destDir, { recursive: true });
      }
      continue;
    }

    for (const sourceFile of walkFiles(hostDir, normalizedSrc)) {
      copyFile(sourceFile, destDir);
    }
  }
}

console.log('Copied client libs to', libsDir);
