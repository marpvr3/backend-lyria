# Building blocks del dominio — Lyria

## Propósito

Este documento describe las abstracciones base del dominio de Lyria: las clases, interfaces y convenciones que sirven como cimientos para todos los agregados, entidades y value objects del sistema. Estas abstracciones viven en `Lyria.Domain.Abstractions` y no dependen de ningún paquete externo.

---

## Abstracciones

### `Entity<TId>`

**Namespace:** `Lyria.Domain.Abstractions`

Clase base abstracta para todas las entidades del dominio. Define identidad tipada y semántica de igualdad por identidad.

**Características:**
- Parámetro genérico `TId` con restricción `notnull`.
- Propiedad `Id` con `protected init` para compatibilidad con ORMs.
- Constructor protegido con parámetro `id` y constructor sin parámetros para hidratación.
- Igualdad por identidad: dos entidades del mismo tipo con el mismo `Id` son iguales.
- Manejo de entidades transitorias: si alguna de las dos entidades tiene `Id` con valor por defecto (`Guid.Empty`, `0`, etc.), la igualdad se resuelve únicamente por referencia.
- Comparación de tipos: dos entidades de tipos distintos nunca son iguales, aunque compartan el mismo `TId`.
- Operadores `==` y `!=` consistentes con `Equals`.

**Reglas de igualdad (8 reglas):**

| # | Escenario | Resultado |
|---|-----------|-----------|
| 1 | Misma referencia | `true` |
| 2 | Comparación con `null` | `false` |
| 3 | Tipo diferente, mismo `Id` | `false` |
| 4 | Mismo tipo, mismo `Id` | `true` |
| 5 | Mismo tipo, diferente `Id` | `false` |
| 6 | Ambas transitorias (Id por defecto) | `false` |
| 7 | Una transitoria, otra no | `false` |
| 8 | Operadores `==`/`!=` consistentes | Sí |

**No incluye:**
- Colección de eventos de dominio (responsabilidad de `AggregateRoot`).
- `CreatedAt`, `UpdatedAt`, `IsDeleted` ni metadatos de auditoría.

---

### `AggregateRoot<TId>`

**Namespace:** `Lyria.Domain.Abstractions`

Clase base abstracta para aggregate roots. Hereda de `Entity<TId>` y agrega la capacidad de registrar eventos de dominio.

**Características:**
- Colección privada `List<IDomainEvent>` para eventos pendientes.
- `DomainEvents`: propiedad pública `IReadOnlyCollection<IDomainEvent>`.
- `RaiseDomainEvent(IDomainEvent)`: método protegido. Rechaza eventos nulos con `ArgumentNullException`.
- `ClearDomainEvents()`: método público que limpia la colección. Diseñado para ser invocado por la infraestructura después de despachar los eventos.

**No incluye:**
- Dispatcher de eventos.
- Outbox pattern.
- Dependencia a MediatR o cualquier librería de mensajería.

---

### `IDomainEvent`

**Namespace:** `Lyria.Domain.Abstractions`

Interfaz marcadora (marker interface) para eventos de dominio. No define propiedades ni métodos.

**Justificación:**
- Los eventos de dominio son hechos de negocio que han ocurrido.
- No se incluye `OccurredOnUtc` porque el timestamp es una preocupación de infraestructura (enriquecimiento al despachar o persistir).
- No depende de MediatR (`INotification`) ni de ningún sistema de mensajería.
- Los tipos concretos de evento se definirán en los módulos de dominio correspondientes.

---

### `ValueObject`

**Namespace:** `Lyria.Domain.Abstractions`

Clase base abstracta para value objects. Define igualdad estructural basada en componentes.

**Características:**
- `GetEqualityComponents()`: método abstracto que cada value object implementa para declarar sus componentes de igualdad.
- Igualdad por valor: dos value objects son iguales si son del mismo tipo y tienen los mismos componentes.
- Comparación de tipos: dos value objects de tipos distintos nunca son iguales, aunque tengan los mismos componentes.
- `GetHashCode()` basado en los mismos componentes.
- Operadores `==` y `!=` consistentes con `Equals`.
- Manejo correcto de comparaciones con `null`.

---

### `IStronglyTypedId<TValue>`

**Namespace:** `Lyria.Domain.Abstractions`

Contrato para identificadores fuertemente tipados. Define la forma que deben seguir los identificadores de entidades y agregados.

**Características:**
- Parámetro genérico `TValue` con restricción `notnull` y covariante (`out`).
- Propiedad `Value` de solo lectura que expone el valor primitivo subyacente.
- Diseñado para ser implementado como `record struct` para obtener igualdad por valor sin costo de heap.
- No impone `Guid` como tipo único: permite `int`, `long`, `string` u otros tipos primitivos.
- No incluye auto-generación, conversores JSON, conversores EF Core ni binding de ASP.NET Core.

**Ejemplo de implementación futura:**

```csharp
public readonly record struct EstablishmentId(Guid Value) : IStronglyTypedId<Guid>;
```

---

### `DomainException`

**Namespace:** `Lyria.Domain.Exceptions`

Clase base abstracta para excepciones de dominio. Representa errores que violan reglas de negocio.

**Características:**
- Hereda de `Exception`.
- Constructor protegido con `message`.
- Constructor protegido con `message` e `innerException`.
- No incluye propiedades HTTP (status code, ProblemDetails).
- No incluye constructores de serialización obsoletos.
- Los tipos concretos de excepción se definirán en los módulos de dominio correspondientes.

---

## Estructura de carpetas

```
src/Lyria.Domain/
├── Abstractions/
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   ├── ValueObject.cs
│   ├── IDomainEvent.cs
│   └── IStronglyTypedId.cs
└── Exceptions/
    └── DomainException.cs
```

---

## Pruebas

Las abstracciones base están cubiertas por pruebas unitarias en `tests/Lyria.Domain.UnitTests/Abstractions/`:

| Categoría | Archivo | Cantidad |
|-----------|---------|----------|
| Entity equality | `EntityEqualityTests.cs` | 8 |
| AggregateRoot events | `AggregateRootDomainEventTests.cs` | 7 |
| ValueObject equality | `ValueObjects/ValueObjectEqualityTests.cs` | 7 |
| StronglyTypedId | `StronglyTypedIdTests.cs` | 4 |
| DomainException | `DomainExceptionTests.cs` | 4 |

Las pruebas de arquitectura en `tests/Lyria.ArchitectureTests/LayerDependencyTests.cs` validan que el dominio no dependa de capas externas ni contenga tipos prohibidos.

---

## Decisiones relacionadas

- [ADR-016: Building blocks del dominio](../adr/ADR-016-domain-building-blocks.md)
- [ADR-002: Clean Architecture](../adr/ADR-002-clean-architecture.md)
- [ADR-005: CQRS y MediatR](../adr/ADR-005-cqrs-mediator.md)
