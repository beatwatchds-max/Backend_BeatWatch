# Security Policy

## Supported Version

Security fixes are applied to the latest version on the `main` branch.

## Reporting a Vulnerability

Do not create a public issue for a suspected vulnerability. Report it privately to the project maintainers with:

- A description of the impact.
- Steps to reproduce the issue.
- A proof of concept when available.
- Any suggested mitigation.

Maintain confidentiality until the maintainers have assessed and remediated the issue. Do not include credentials, access tokens, or personal health data in the report.

## Security Controls

Pull requests to protected branches are checked with dependency auditing, secret scanning, CodeQL, tests, and a container smoke test. Production images are published with an SBOM and provenance attestation.
