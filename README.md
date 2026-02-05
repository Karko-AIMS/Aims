# AIMS - Autonomous Information Management System

**Multi-Tenant 기반 차량 운영·정비 관리 시스템**

AIMS는 조직(Organization) 단위로 차량(Vehicle)과 운영 데이터를 관리하기 위한 엔터프라이즈급 관리 시스템입니다.  
백엔드 API(.NET 10) + 데스크톱 클라이언트(WPF .NET 10) 구조로 구성되며, 운영/정비/관제 중심의 MVP를 목표로 설계되었습니다.

---

## 🎯 Key Features

### Multi-Tenant Architecture
- **조직 단위 데이터 격리**: 모든 운영 리소스는 Organization 단위로 완전히 격리
- **테넌트 간 데이터 보안**: 구조적으로 다른 조직의 데이터 접근 차단
- **전역 관리자 분리**: InternalAdmin은 조직에 소속되지 않으며 시스템 레벨 관리만 수행

### Role-Based Access Control
| Role | Organization | Permissions |
|------|--------------|-------------|
| **InternalAdmin** | None (Global) | Organization 생성/관리 전용 |
| **Operator** | Required | Vehicle CRUD, 전체 운영 기능 |
| **Viewer** | Required | 조회 전용 |

### Vehicle Management
- Organization-bound 차량 관리
- Soft Delete 기반 비활성화/복구
- 역할 기반 CRUD 권한 제어
- 검색 및 필터링 지원

---

## 🏗️ Architecture

### Tech Stack

**Backend**
- ASP.NET Core (.NET 10)
- PostgreSQL with EF Core
- JWT Bearer Authentication
- Scalar OpenAPI Documentation

**Frontend**
- WPF (.NET 10 Windows)
- DPAPI 기반 토큰 보안 저장
- Semi-MVVM 패턴
- Services 레이어 분리 아키텍처

### Project Structure
```
Aims/
├── Aims.Api/                    # Backend API Server
│   ├── Controllers/            # REST API Endpoints
│   ├── Data/                   # EF Core DbContext & Entities
│   ├── Services/               # Business Logic
│   └── Contracts/              # DTOs & Request/Response Models
│
└── Aims.Wpf/                   # WPF Desktop Client
    ├── Services/               # API Integration & Auth
    │   ├── ApiClient.cs        # HTTP Client with Bearer Token
    │   ├── AuthService.cs      # Authentication
    │   ├── VehicleService.cs   # Vehicle CRUD
    │   └── TokenStore.cs       # Secure Token Storage (DPAPI)
    ├── MainWindow.xaml         # Login Screen
    ├── VehicleStatusWindow.xaml # Main Dashboard
    └── VehicleManagementWindow.xaml # Vehicle CRUD UI
```

---

## 🔐 Authentication & Authorization

### JWT Token Structure
```json
{
  "sub": "user-guid",
  "role": "Operator|Viewer|InternalAdmin",
  "orgId": "org-guid"  // InternalAdmin에는 포함되지 않음
}
```

### Access Control Rules
- **Organization 격리**: 사용자는 자신의 Organization 리소스만 접근
- **InternalAdmin 제약**: Vehicle API 접근 불가 (Organization 관리 전용)
- **비활성 Organization**: 소속 사용자 로그인 차단

### API Response Codes
- `401 Unauthorized`: 인증 실패 또는 토큰 만료
- `403 Forbidden`: 권한 부족 또는 Organization 접근 제한
- `404 Not Found`: 리소스 없음 또는 다른 Organization 소유

---

## 🚀 Getting Started

### Prerequisites
- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- [EF Core CLI](https://docs.microsoft.com/ef/core/cli/dotnet)

### 1. Database Setup

**appsettings.json 설정**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aims;Username=postgres;Password=your_password"
  }
}
```

**Migration 실행**
```bash
dotnet ef database update -p Aims.Api -s Aims.Api
```

### 2. Backend API 실행
```bash
cd Aims.Api
dotnet run
```

API: `https://localhost:5001`  
API Docs (Dev): `https://localhost:5001/scalar`

### 3. Development Seed (Optional)

**개발용 전역 관리자 계정 생성**
```bash
POST /api/auth/seed-dev-admin
```
- Development 환경에서만 동작
- users 테이블이 비어있을 때만 허용

### 4. WPF Client 실행

**App.config 설정**
```xml
<appSettings>
  <add key="ApiBaseUrl" value="https://localhost:5001" />
</appSettings>
```
```bash
cd Aims.Wpf
dotnet run
```

---

## 📋 API Endpoints

### Authentication
```
POST   /api/auth/login              # 로그인
POST   /api/auth/register           # 회원가입 (InternalAdmin 전용)
GET    /api/auth/me                 # 현재 사용자 정보
POST   /api/auth/seed-dev-admin     # Dev 전용 시드
```

### Organizations (InternalAdmin Only)
```
GET    /api/organizations           # 조직 목록
POST   /api/organizations           # 조직 생성
PUT    /api/organizations/{id}      # 조직 수정
DELETE /api/organizations/{id}      # 조직 비활성화
POST   /api/organizations/{id}/restore  # 조직 복원
```

### Vehicles (Operator, Viewer)
```
GET    /api/vehicles                # 목록 조회 (검색/필터/페이징)
GET    /api/vehicles/{id}           # 상세 조회
POST   /api/vehicles                # 생성 (Operator)
PUT    /api/vehicles/{id}           # 수정 (Operator)
DELETE /api/vehicles/{id}           # 삭제 (Operator)
POST   /api/vehicles/{id}/restore   # 복원 (Operator)
```

---

## 🔧 Development

### Entity Relationship
```
Organization (1) ──── (*) User
                 └──── (*) Vehicle

- InternalAdmin: Organization = null
- Operator/Viewer: Organization 필수
```

### Database Constraints
- **Organization.Code**: Unique (조직 코드)
- **User.Email**: Unique (이메일 중복 불가)
- **Vehicle.VehicleCode**: Organization 내 Unique
- **Vehicle.VIN**: 전역 Unique (Optional)
- **Vehicle.PlateNumber**: 전역 Unique (Optional)

### Soft Delete Pattern
```csharp
public bool IsActive { get; set; } = true;
public DateTime? DeactivatedAtUtc { get; set; }
```
- Delete 시 `IsActive = false` + `DeactivatedAtUtc` 설정
- Restore 시 `IsActive = true` + `DeactivatedAtUtc = null`

---

## 🛡️ Security Features

### WPF Client
- **DPAPI 암호화**: 토큰을 Windows DPAPI로 암호화 저장
- **Auto Login**: 안전한 토큰 저장 기반 자동 로그인
- **Remember Me**: 이메일 저장 옵션

### Backend API
- **Password Hashing**: BCrypt 기반 해시
- **JWT Validation**: 모든 엔드포인트 검증
- **Organization Scope**: 쿼리 필터 자동 적용
- **Role Enforcement**: Attribute 기반 권한 체크

---

## 📝 Development Roadmap

### Current (MVP)
- [x] Multi-tenant Organization 관리
- [x] JWT 인증/인가
- [x] Vehicle CRUD
- [x] Role 기반 접근 제어
- [x] WPF 클라이언트 기본 UI

### Planned
- [ ] 정비 이력 관리 (Maintenance Records)
- [ ] 운행 일지 (Operation Logs)
- [ ] 대시보드 & 통계
- [ ] 알림 시스템
- [ ] 감사 로그 (Audit Trail)

---

## 📄 License

이 프로젝트는 내부 사용을 위한 프로젝트입니다.

---

## 👥 Contributors

- **Jongmin Choi** - Initial work & Architecture

---

## 📞 Support

문의사항이 있으시면 이슈를 등록해주세요.
