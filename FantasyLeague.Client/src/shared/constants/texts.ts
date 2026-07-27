export const texts = {
  brand: { primary: 'HOOP', accent: 'BASE', breadcrumb: 'Hoopbase' },
  accessibility: {
    openMenu: 'Menüyü aç',
    closeMenu: 'Menüyü kapat',
    notifications: 'Bildirimler',
    leagueMenu: 'Lig menüsü',
  },
  navigation: {
    title: 'MENÜ',
    overview: 'Genel Bakış',
    leagues: 'Liglerim',
    team: 'Takımım',
    players: 'Oyuncular',
    transfers: 'Transferler',
  },
  actions: { createLeague: 'Yeni lig', viewAll: 'Tümünü gör', details: 'Lig detayları' },
  dashboard: {
    eyebrow: 'FANTASY BASKETBOL',
    greeting: 'Sahaya çık',
    subtitle: 'Takımını kur, hamleni yap ve zirveye oyna.',
    sectionLabel: 'LİGLERİM',
    sectionTitle: 'Rekabet burada başlıyor.',
    loading: 'Ligler yükleniyor…',
    emptyTitle: 'Henüz bir ligin yok',
    emptyText: 'İlk ligini oluşturarak rekabeti başlat.',
    stats: {
      totalLeagues: 'TOPLAM LİG',
      currentSeason: 'Tüm sezonlar',
      activeLeagues: 'AKTİF LİG',
      live: 'Canlı',
    },
  },
  league: {
    seasonSuffix: 'SEZONU',
    defaultDescription: 'Fantasy basketbol ligi',
    statuses: {
      Created: 'Oluşturuldu',
      RegistrationOpen: 'Kayıt Açık',
      DraftDelayed: 'Draft Ertelendi',
      Drafting: 'Draft Zamanı',
      Active: 'Aktif Sezon',
      Completed: 'Tamamlandı',
    },
  },
  errors: { requestFailed: 'The request could not be completed.' },
} as const

export type AppTexts = typeof texts
