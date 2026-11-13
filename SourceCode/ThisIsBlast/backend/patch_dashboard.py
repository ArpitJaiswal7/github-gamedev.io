with open('backend/Dashboard/index.html', 'r', encoding='utf-8') as f:
    c = f.read()

target = """        const archIcon = arch.archetypeId.includes('sawtooth') ? 'activity' : 'trending-up';
        const archColor = arch.archetypeId.includes('sawtooth') ? 'text-amber-400' : 'text-blue-400';
        const archBadgeBg = arch.archetypeId.includes('sawtooth') ? 'bg-amber-500/10 text-amber-300 border-amber-500/20' : 'bg-blue-500/10 text-blue-300 border-blue-500/20';"""

replacement = """        const archIdLower = (arch.archetypeId || '').toLowerCase();
        let archIcon = 'trending-up';
        let archColor = 'text-blue-400';
        let archBadgeBg = 'bg-blue-500/10 text-blue-300 border-blue-500/20';
        let archDesc = 'Monotonic smooth difficulty ramp';

        if (archIdLower.includes('sawtooth')) {
          archIcon = 'activity';
          archColor = 'text-amber-400';
          archBadgeBg = 'bg-amber-500/10 text-amber-300 border-amber-500/20';
          archDesc = 'Climax & breather periodic wave pacing';
        } else if (archIdLower.includes('royal') || archIdLower.includes('rush')) {
          archIcon = 'crown';
          archColor = 'text-purple-400';
          archBadgeBg = 'bg-purple-500/10 text-purple-300 border-purple-500/20';
          archDesc = 'High-octane dopamine loops & rapid cascade merge pacing';
        }"""

old_desc = """                    ${arch.archetypeId.includes('sawtooth') ? 'Climax & breather periodic wave pacing' : 'Monotonic smooth difficulty ramp'}"""
new_desc = """                    ${archDesc}"""

if target in c:
    c = c.replace(target, replacement)
if old_desc in c:
    c = c.replace(old_desc, new_desc)

with open('backend/Dashboard/index.html', 'w', encoding='utf-8') as f:
    f.write(c)

print('Updated Dashboard HTML styling successfully!')
