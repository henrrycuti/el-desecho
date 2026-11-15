# Estructura por capítulos

Guía de diseño para construir El Desecho capítulo a capítulo. La historia completa está en [HISTORIA_EL_DESECHO.md](HISTORIA_EL_DESECHO.md) y los ajustes acordados en [DECISIONES_HISTORIA.md](DECISIONES_HISTORIA.md).

## Lo que tomamos de otros juegos por episodios

- **Cada capítulo es una historia corta con su propio cierre.** Los juegos de terror por episodios terminan cada capítulo con un momento fuerte que empuja al siguiente, como una serie de televisión. En El Desecho: el prólogo cierra con la puerta del laboratorio entreabierta; el capítulo 1 con la puerta de mantenimiento y algo golpeando detrás; el 2 con el conducto estrecho; el 3 con la llave y el antídoto; el 4 al llegar a la puerta pesada.
- **Tres actos dentro de cada capítulo.** Un tramo para aprender o descubrir, un tramo con varias rutas y un final de tensión (persecución o huida).
- **Puntos de control frecuentes.** Repetir tramos largos tras morir cansa; cada zona nueva guarda la partida.
- **Elegir capítulo al terminar.** Cuando un capítulo se completa, el menú permite volver a jugarlo.

## Cómo guiamos al jugador por las alcantarillas

Las alcantarillas son un laberinto, pero el jugador nunca debe sentirse perdido sin motivo:

- **Ruta principal clara:** el camino que hace avanzar la historia. Siempre hay algo que la señala: el GPS, las luces de emergencia o las marcas de arrastre.
- **Puntos de referencia:** lugares que se reconocen de lejos (la cámara grande con luz verde, la luz roja de la puerta del laboratorio) para orientarse.
- **Desvíos con premio o con peligro:** callejones sin salida, compuertas cerradas y, más adelante, objetos útiles o trampas.
- **Bloqueos con llave:** puertas que solo se abren con un objeto (la llave del capítulo 3), para que el mundo se abra por partes.

## Sistemas del juego (Assets/ElDesecho/Scripts)

| Sistema | Qué hace |
| --- | --- |
| `ChapterManager` | Lista de capítulos, partida guardada, fundido a negro y título del capítulo al cambiar de zona. |
| `ObjectiveManager` | Pasos del guion de cada escena: objetivo arriba, subtítulos, marcador y ruta del GPS. |
| `StoryTrigger` | Zona invisible que avisa a la historia cuando el jugador entra. |
| `Interactable` | Tapas, escaleras, puertas y objetos que se usan mirándolos (E o botón USAR). |
| `FirstPersonController` | Jugador: caminar, correr, agacharse, mirar, linterna y usar. Teclado y ratón, o controles táctiles. |
| `Hud` y `MainMenu` | Pantalla de juego, pausa y menú inicial. |
| `ElDesechoBuilder` (Editor) | Construye las escenas del prólogo y el kit modular de alcantarilla. |

## Kit modular de alcantarilla (Assets/ElDesecho/Prefabs/Alcantarilla)

Piezas de 6 × 6 m que encajan entre sí: `Tunel_Recto`, `Tunel_Curva`, `Tunel_T`, `Tunel_Cruce`, `Tunel_Final`, `Camara_Pieza` (cámara de techo alto), `Pasarela_Barandilla`, `Escalera_Mano` y `Compuerta`. Los túneles tienen agua a la altura del tobillo.

## Plan de capítulos

| Capítulo | Zonas | Objetivo principal | Mecánica nueva | Final |
| --- | --- | --- | --- | --- |
| Prólogo: La misión | Calle con lluvia → alcantarilla | Encontrar la entrada de servicio y seguir el GPS | Moverse, linterna, usar objetos, GPS | La puerta del laboratorio, entreabierta |
| 1: El laboratorio silencioso | Laboratorio destrozado → pasillos rojos | Encontrar una linterna de emergencia y la salida a las alcantarillas | Esconderse y evitar a la criatura | Puerta de mantenimiento; algo golpea detrás |
| 2: Tres caminos | Alcantarillas en tres ramas | Elegir el camino que sigue al exterior | Decisión de ruta, adrenalina | Huida por el conducto estrecho |
| 3: Lo que queda de la investigación | Sección amplia, sala de vigilancia | Encontrar la llave de la puerta reforzada | Gafas de exploración, antídoto | La puerta reforzada se abre |
| 4: La puerta de salida | Sectores de mantenimiento inundados y secos | Llegar a la puerta pesada | Sigilo, vapor, agua con huecos | Llegar a la puerta, sin abrirla |
| 5: La luz del día | Túnel de la persecución final | Escapar hasta la compuerta abierta | Persecución con antídoto y adrenalina | La luz del amanecer |
| Epílogo | Noticias, institución, túneles | Cinemática | — | La masa crece y ruge |

## Estado

- Prólogo: jugable con geometría provisional (formas simples). Faltan sonido, modelos definitivos y la cinemática del sobre.
- Capítulos 1 a 5 y epílogo: por construir.
- El Deshecho: modelo traído de UMBRA, colocable en escena y en reposo. Sin inteligencia todavía.

## Fuentes consultadas

- [Directive 8020 y su estructura por episodios (PC Gamer)](https://www.pcgamer.com/games/horror/directive-8020-is-using-an-episodic-structure-straight-out-of-a-limited-tv-series-to-be-mindful-of-peoples-time/)
- [Diseño de niveles de Die Young: Prologue en tres actos (Game Developer)](https://gamedeveloper.com/design/a-three-level-short-story---the-level-design-of-die-young-prologue)
- [Tensión y persecuciones en el diseño de niveles de terror (Game Developer)](https://gamedeveloper.com/design/creating-horror-through-level-design-tension-jump-scares-and-chase-sequences)
- [Capítulos de Little Nightmares 3](https://space4games.com/en/games-en/little-nightmares-3-every-chapter-and-whats-coming-next/)
- [Guiar al jugador sin decírselo (Unity)](https://unity.com/blog/level-design-basics)
- [Tipos de distribución de niveles (The Level Design Book)](https://book.leveldesignbook.com/process/layout/typology)
