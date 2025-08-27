# Security Policy

## Overview

Claude Profile Manager for Windows is designed with security as a foundational principle. This document outlines our security practices, architecture decisions, and procedures for reporting vulnerabilities.

## Security Architecture

### Credential Storage Security

**Windows Credential Manager Integration**
- All sensitive credentials stored in Windows Credential Manager
- Utilizes Windows Data Protection API (DPAPI) for encryption
- Per-user credential isolation - no cross-user access
- Automatic encryption/decryption using user's Windows credentials
- No plain text storage of API keys or authentication tokens

**Storage Isolation**
```
Credential Keys:
- Console API: "{profile-name}-console" 
- Subscription: "{profile-name}-subscription"
- Stored Target: "Claude Profile Manager"
```

### File System Security

**Access Control Lists (ACLs)**
- Profile metadata files have restrictive ACLs
- Access limited to current user + SYSTEM account only
- Inheritance disabled to prevent permission escalation
- Automatic permission verification and repair

**File Locations**
- Profile metadata: `%USERPROFILE%\.claude\profiles\`
- Configuration: `%USERPROFILE%\.claude\config\`
- No system-wide or shared storage locations

### Input Validation and Sanitization

**Comprehensive Input Validation**
- Profile names validated against reserved Windows names
- Path traversal prevention (../, ..\, absolute paths)
- Command injection protection
- SQL injection pattern detection (defensive programming)
- Maximum length restrictions to prevent buffer overflow

**Pattern Detection**
Using source-generated regex for performance:
```csharp
[GeneratedRegex(@"[<>:""|?*]|^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase)]
private static partial Regex InvalidNamePattern();
```

### Information Disclosure Prevention

**Exception Sanitization**
- Automatic credential removal from all error messages
- Path sanitization in development/debug scenarios  
- Secure logging with credential pattern detection
- No sensitive data in stack traces or logs

**Logging Security**
```csharp
// Credentials automatically sanitized from all log output
_logger.LogError("Operation failed for profile {ProfileName}", 
    InputValidator.SanitizeForLogging(profileName));
```

### Process and Memory Security

**Secure Memory Handling**
- SecureString support for credential handling
- Automatic memory cleanup for sensitive data
- No credential caching in managed memory
- Immediate disposal of sensitive objects

**Process Isolation**
- No credential exposure in process command lines
- Environment variable isolation
- Secure process startup for external tool integration

## Authentication Method Support

### Console API Keys
- Static API keys stored in Windows Credential Manager
- One-time storage with manual rotation
- Validation against known API key formats
- Secure retrieval for Claude Code CLI integration

### Subscription OAuth Tokens
- Dynamic OAuth token management
- Token health monitoring and expiration detection
- Auto-save functionality to prevent token loss
- Secure token refresh handling (when implemented)

### Token Validation
```csharp
// OAuth token structure validation
private static readonly Regex OAuthTokenPattern = new(
    @"^\{.*""claudeAiOauth"".*\}$", 
    RegexOptions.Compiled | RegexOptions.IgnoreCase);
```

## Threat Model and Mitigations

### Threat: Credential Theft
**Mitigation**: Windows Credential Manager encryption, ACL-protected metadata

### Threat: Profile Enumeration
**Mitigation**: User-only file access, no system-wide profile listing

### Threat: Injection Attacks  
**Mitigation**: Comprehensive input validation, parameterized operations

### Threat: Information Disclosure
**Mitigation**: Exception sanitization, secure logging practices

### Threat: Privilege Escalation
**Mitigation**: Standard user permissions, no admin requirements

### Threat: Data Tampering
**Mitigation**: ACL protection, atomic file operations, integrity verification

## Vulnerability Disclosure

### Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

### Reporting Security Issues

**Please DO NOT report security vulnerabilities through public GitHub issues.**

Instead, please report them responsibly:

1. **Email**: Send details to security@claude-profile-manager.dev
2. **Subject**: "Security Vulnerability Report - Claude Profile Manager"
3. **Include**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact assessment
   - Suggested remediation (if known)

### Response Timeline

- **Acknowledgment**: Within 24 hours
- **Initial Assessment**: Within 72 hours  
- **Status Updates**: Weekly until resolution
- **Fix Timeline**: Critical issues within 7 days, others within 30 days

### Responsible Disclosure

We follow responsible disclosure practices:

1. **Coordinated Disclosure**: Work together on timeline
2. **Credit**: Security researchers credited unless they prefer anonymity
3. **CVE Assignment**: For significant vulnerabilities
4. **Public Disclosure**: After fix is available and deployed

## Security Best Practices for Users

### Installation Security
- Download only from official sources
- Verify digital signatures before installation
- Use standard user account (avoid running as administrator)
- Keep Windows and .NET runtime updated

### Profile Management Security
- Use strong, unique profile names
- Regularly rotate API keys and tokens
- Monitor credential usage in Claude Code CLI
- Delete unused profiles promptly

### System Hardening
- Enable Windows Credential Manager auditing
- Monitor `%USERPROFILE%\.claude\` directory access
- Use Windows Defender or equivalent antivirus
- Regular system updates and security patches

## Compliance and Standards

### Security Standards
- **OWASP Top 10**: Mitigations implemented for all categories
- **CWE**: Common Weakness Enumeration compliance
- **Microsoft Security Development Lifecycle**: Followed during development

### Privacy Protection
- **Data Minimization**: Only necessary data stored
- **Purpose Limitation**: Data used only for profile management
- **User Control**: Users control all profile data and deletion

### Audit and Monitoring
- Optional audit logging available via environment variables
- Windows Event Log integration for security events
- File system audit trail support

## Security Testing

### Static Analysis
- Comprehensive static analysis during build
- Dependency vulnerability scanning
- Code quality and security rule enforcement

### Dynamic Testing
- Runtime security testing
- Credential isolation verification
- Permission boundary testing

### Penetration Testing
Regular security assessments including:
- Injection attack testing
- Privilege escalation attempts
- Information disclosure verification
- Credential protection validation

## Contact Information

- **Security Team**: security@claude-profile-manager.dev
- **General Issues**: https://github.com/derekspelledcorrectly/claude-profile-manager/issues
- **Documentation**: https://github.com/derekspelledcorrectly/claude-profile-manager/wiki

## Security Updates

Subscribe to security updates:
- **GitHub Releases**: Watch repository for security releases
- **Security Advisories**: GitHub Security Advisories
- **Changelog**: Review CHANGELOG.md for security-related updates

---

**Last Updated**: 2025-08-26
**Version**: 1.0.0