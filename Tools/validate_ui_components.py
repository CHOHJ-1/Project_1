#!/usr/bin/env python3
"""Check serialized Unity component ownership and duplicate UI renderers. No extra packages."""
from pathlib import Path
import re

REPOSITORY = Path(__file__).resolve().parents[1]

def validate(text, label):
    blocks = re.split(r'^--- !u!(\d+) &(\d+)\n', text, flags=re.MULTILINE)
    documents = {}
    for i in range(1, len(blocks), 3):
        kind, identifier, body = int(blocks[i]), int(blocks[i + 1]), blocks[i + 2]
        assert identifier not in documents, f'{label}: duplicate fileID {identifier}'
        documents[identifier] = (kind, body)
    owners = {}
    renderer_count = 0
    for identifier, (kind, body) in documents.items():
        if kind != 1:
            continue
        name = re.search(r'^  m_Name: (.*)$', body, re.MULTILINE).group(1)
        references = re.findall(r'^  - component: \{fileID: (\d+)\}', body, re.MULTILINE)
        assert len(references) == len(set(references)), f'{label}/{name}: repeated component reference'
        renderers = []
        scripts = []
        for reference in references:
            cid = int(reference)
            assert cid in documents, f'{label}/{name}: missing component {cid}'
            assert cid not in owners, f'{label}/{name}: component shared across objects {cid}'
            owners[cid] = identifier
            component_kind, component_body = documents[cid]
            owner = re.search(r'^  m_GameObject: \{fileID: (\d+)\}', component_body, re.MULTILINE)
            assert owner and int(owner.group(1)) == identifier, f'{label}/{name}: wrong component owner {cid}'
            if component_kind in (23, 96, 137, 199, 212, 222):
                renderers.append(component_kind)
            script = re.search(r'^  m_Script: \{fileID: 11500000, guid: ([a-f0-9]+), type: 3\}', component_body, re.MULTILINE)
            if script:
                scripts.append(script.group(1))
        assert len(renderers) <= 1, f'{label}/{name}: duplicate renderers {renderers}'
        assert len(scripts) == len(set(scripts)), f'{label}/{name}: duplicate script components'
        renderer_count += len(renderers)
    for identifier, (kind, body) in documents.items():
        if re.search(r'^  m_GameObject: ', body, re.MULTILINE):
            assert identifier in owners, f'{label}: orphaned component {identifier}'
    return len(owners), renderer_count

def main():
    assets = sorted(REPOSITORY.glob('Assets/**/*.unity')) + sorted(REPOSITORY.glob('Assets/**/*.prefab'))
    assert assets, 'No scene or prefab assets found'
    for asset in assets:
        components, renderers = validate(asset.read_text(encoding='utf-8'), str(asset.relative_to(REPOSITORY)))
        print(f'PASS {asset.relative_to(REPOSITORY)}: {components} components, {renderers} renderers, no duplicates or ownership errors')

if __name__ == '__main__':
    main()
